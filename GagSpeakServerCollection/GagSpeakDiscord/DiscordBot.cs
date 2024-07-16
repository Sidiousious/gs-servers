
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using GagspeakDiscord.Commands;
using GagspeakDiscord.Modules.AccountWizard;
using GagspeakDiscord.Modules.KinkDispenser;
using GagspeakServer.Hubs;
using GagspeakShared.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using GagspeakShared.Data;

namespace GagspeakDiscord;

internal class DiscordBot : IHostedService
{
    private readonly DiscordBotServices _botServices;                               // The class for the discord bot services
    private readonly IConfigurationService<DiscordConfiguration> _discordConfigService;    // The configuration service for the discord bot
    private readonly IConnectionMultiplexer _connectionMultiplexer;                 // the connection multiplexer for the discord bot
    private readonly DiscordSocketClient _discordClient;                            // the discord client socket
    private readonly ILogger<DiscordBot> _logger;                                   // the logger for the discord bot
    private readonly IHubContext<GagspeakHub> _gagspeakHubContext;                  // the hub context for the gagspeak hub
    private readonly IServiceProvider _services;                                    // the service provider for the discord bot
    private InteractionService _interactionModule;                                  // the interaction module for the discord bot
    private CancellationTokenSource? _processReportQueueCts;                        // the CTS for the process report queues
    private CancellationTokenSource? _updateStatusCts;                              // the CTS for the update status
    private CancellationTokenSource? _vanityUpdateCts;                              // the CTS for the vanity update

    public DiscordBot(DiscordBotServices botServices, IServiceProvider services, IConfigurationService<DiscordConfiguration> configuration,
        IHubContext<GagspeakHub> gagspeakHubContext, ILogger<DiscordBot> logger, IConnectionMultiplexer connectionMultiplexer)
    {
        _botServices = botServices;
        _services = services;
        _discordConfigService = configuration;
        _gagspeakHubContext = gagspeakHubContext;
        _logger = logger;
        _connectionMultiplexer = connectionMultiplexer;
        // Create a new discord client with the default retry mode
        _discordClient = new(new DiscordSocketConfig()
        {
            DefaultRetryMode = RetryMode.AlwaysRetry
        });

        // subscribe to the log event from discord.
        _discordClient.Log += Log;
    }

    // the main startup function, this will run whenever the discord bot is turned on
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // get the discord bot token from the configuration
        var token = _discordConfigService.GetValueOrDefault(nameof(DiscordConfiguration.DiscordBotToken), string.Empty);
        // if the token is not empty
        if (!string.IsNullOrEmpty(token))
        {
            // log the information that the discord bot is starting
            _logger.LogInformation("Starting DiscordBot");
            _logger.LogInformation("Using Configuration: " + _discordConfigService.ToString());

            // create a new interaction module for the discord bot
            _interactionModule = new InteractionService(_discordClient);

            // subscribe to the log event from the interaction module
            _interactionModule.Log += Log;

            // next we will want to add the modules to the interaction module.
            // These will be responsible for generating the UID's
            // (at least for now, that purpose will change later)
            await _interactionModule.AddModuleAsync(typeof(GagspeakCommands), _services).ConfigureAwait(false);
            await _interactionModule.AddModuleAsync(typeof(AccountWizard), _services).ConfigureAwait(false);
            await _interactionModule.AddModuleAsync(typeof(KinkDispenser), _services).ConfigureAwait(false);

            // log the bot into to the discord client with the token
            await _discordClient.LoginAsync(TokenType.Bot, token).ConfigureAwait(false);
            // start the discord client
            await _discordClient.StartAsync().ConfigureAwait(false);

            // subscribe to the ready event from the discord client
            _discordClient.Ready += DiscordClient_Ready;

            // subscribe to the interaction created event from the discord client (occurs when player interacts with its posted events.)
            _discordClient.InteractionCreated += async (x) =>
            {
                var ctx = new SocketInteractionContext(_discordClient, x);
                await _interactionModule.ExecuteCommandAsync(ctx, _services).ConfigureAwait(false);
            };

            // start the bot services
            await _botServices.Start().ConfigureAwait(false);

            // update the status of the bot
            _ = UpdateStatusAsync();
        }
    }

    /// <summary>
    /// Stops the discord bot
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // if the discord bot token is not empty
        if (!string.IsNullOrEmpty(_discordConfigService.GetValueOrDefault(nameof(DiscordConfiguration.DiscordBotToken), string.Empty)))
        {
            // await for all bot services to stop
            await _botServices.Stop().ConfigureAwait(false);

            // cancel the process report queue CTS, and all other CTS tokens
            _processReportQueueCts?.Cancel();
            _updateStatusCts?.Cancel();
            _vanityUpdateCts?.Cancel();

            // await for the bot to logout
            await _discordClient.LogoutAsync().ConfigureAwait(false);
            // await for the bot to stop
            await _discordClient.StopAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// The ready event for the discord client
    /// </summary>
    private async Task DiscordClient_Ready()
    {
        var guilds = await _discordClient.Rest.GetGuildsAsync().ConfigureAwait(false);
        foreach (var guild in guilds)
        {
            await _interactionModule.RegisterCommandsToGuildAsync(guild.Id, true).ConfigureAwait(false);
            await CreateOrUpdateModal(guild).ConfigureAwait(false);
            _ = UpdateVanityRoles(guild);
            _ = RemovePerksFromUsersNotInVanityRole();
        }
    }

    /// <summary> The primary function for creating / updating the account claim system </summary>
    private async Task CreateOrUpdateModal(RestGuild guild)
    {
        // log that we are creating the account management system channel
        _logger.LogDebug("Account Management Wizard: Getting Channel");

        // fetch the channel for the account management system message
        var discordChannelForCommands = _discordConfigService.GetValue<ulong?>(nameof(DiscordConfiguration.DiscordChannelForCommands));
        if (discordChannelForCommands == null)
        {
            _logger.LogWarning("Account Management Wizard: No channel configured");
            return;
        }

        // create the message
        IUserMessage? message = null;
        var socketchannel = await _discordClient.GetChannelAsync(discordChannelForCommands.Value).ConfigureAwait(false) as SocketTextChannel;
        var pinnedMessages = await socketchannel.GetPinnedMessagesAsync().ConfigureAwait(false);
        // check if the message is already pinned
        foreach (var msg in pinnedMessages)
        {
            // log the information that we are checking the message id
            _logger.LogDebug("Account Management Wizard: Checking message id {id}, author is: {author}, hasEmbeds: {embeds}", msg.Id, msg.Author.Id, msg.Embeds.Any());
            // if the author of the post is the bot, and the message has embeds
            if (msg.Author.Id == _discordClient.CurrentUser.Id && msg.Embeds.Any())
            {
                // then get the message 
                message = await socketchannel.GetMessageAsync(msg.Id).ConfigureAwait(false) as IUserMessage;
                break;
            }
        }

        // and log it.
        _logger.LogInformation("Account Management Wizard: Found message id: {id}", message?.Id ?? 0);

        // then generate our accountWizard message in the channel we just inspected
        await GenerateOrUpdateWizardMessage(socketchannel, message).ConfigureAwait(false);
    }

    /// <summary> The primary account management wizard for the discord. Nessisary for claiming accounts </summary>
    private async Task GenerateOrUpdateWizardMessage(SocketTextChannel channel, IUserMessage? prevMessage)
    {
        // construct the embed builder
        EmbedBuilder eb = new EmbedBuilder();
        eb.WithTitle("Official CK GagSpeak Bot Service");
        eb.WithDescription("Press \"Start\" to interact with me!" + Environment.NewLine + Environment.NewLine
            + "You can handle all of your GagSpeak account needs in this server.\nJust follow the instructions!");
        eb.WithThumbnailUrl("https://raw.githubusercontent.com/CordeliaMist/GagSpeak-Client/main/images/iconUI.png");
        // construct the buttons
        var cb = new ComponentBuilder();
        // this claim your account button will trigger the customid of wizard-home:true, letting the bot deliever a personalized reply
        // that will display the account information.
        cb.WithButton("Start GagSpeak Account Management", style: ButtonStyle.Primary, customId: "wizard-home:true", emote: Emoji.Parse("🎀"));
        // if the previous message is null
        if (prevMessage == null)
        {
            // send the message to the channel
            var msg = await channel.SendMessageAsync(embed: eb.Build(), components: cb.Build()).ConfigureAwait(false);
            try
            {
                // pin the message
                await msg.PinAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // swallow
            }
        }
        else // if message is already generated, just modify it.
        {
            await prevMessage.ModifyAsync(p =>
            {
                p.Embed = eb.Build();
                p.Components = cb.Build();
            }).ConfigureAwait(false);
        }
        // once the button is pressed, it should display the main menu to the user.
    }

    /// <summary> Translate discords log messages into our logger format </summary>
    private Task Log(LogMessage msg)
    {
        switch (msg.Severity)
        {
            case LogSeverity.Critical:
            case LogSeverity.Error:
                _logger.LogError(msg.Exception, msg.Message); break;
            case LogSeverity.Warning:
                _logger.LogWarning(msg.Exception, msg.Message); break;
            default:
                _logger.LogInformation(msg.Message); break;
        }

        return Task.CompletedTask;
    }


    /// <summary>
    /// Updates the vanity roles in the concurrent dictionary for the bot services to reflect the list in the appsettings.json
    /// </summary>
    private async Task UpdateVanityRoles(RestGuild guild)
    {
        // while the update status CTS is not requested
        while (!_updateStatusCts.IsCancellationRequested)
        {
            try
            {
                // begin to update the vanity roles. 
                _logger.LogInformation("Updating Vanity Roles");
                // fetch the roles from the configuration list.
                Dictionary<ulong, string> vanityRoles = _discordConfigService.GetValueOrDefault(nameof(DiscordConfiguration.VanityRoles), new Dictionary<ulong, string>());
                // if the vanity roles are not the same as the list fetched from the bot service,
                if (vanityRoles.Keys.Count != _botServices.VanityRoles.Count)
                {
                    // clear the roles in the bot service, and create a new list
                    _botServices.VanityRoles.Clear();
                    // for each role in the list of roles
                    foreach (var role in vanityRoles)
                    {
                        _logger.LogDebug("Adding Role: {id} => {desc}", role.Key, role.Value);
                        // add the ID and the name of the role to the bot service.
                        var restrole = guild.GetRole(role.Key);
                        if (restrole != null) _botServices.VanityRoles.Add(restrole, role.Value);
                    }
                }

                // schedule this task every 5 minutes. (since i dont think we will need it often or ever change it.
                await Task.Delay(TimeSpan.FromSeconds(300), _updateStatusCts.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during UpdateVanityRoles");
            }
        }
    }

    /// <summary> 
    /// Removes the VanityPerks from users who are no longer supporting CK 
    /// <para>
    /// This does NOT remove the user from the database, nor does it remove them from the discord, nor their roles.
    /// It will ONLY REMOVE THE PERKS ASSOCIATED WITH BEING ONE
    /// </para>
    /// </summary>
    private async Task RemovePerksFromUsersNotInVanityRole()
    {
        // refresh the CTS for our vanity updates.
        _vanityUpdateCts?.Cancel();
        _vanityUpdateCts?.Dispose();
        _vanityUpdateCts = new();
        // set the token to the token
        var token = _vanityUpdateCts.Token;
        // set the guild to the guild ID of Cordy's Kinkporium
        var restGuild = await _discordClient.Rest.GetGuildAsync(878511238764720129);
        // set the application ID to the application ID of the bot
        var appId = await _discordClient.GetApplicationInfoAsync().ConfigureAwait(false);

        // while the cancellation token is not requested
        while (!token.IsCancellationRequested)
        {
            // try and clean up the vanity UID's from people no longer Supporting CK
            try
            {
                _logger.LogInformation("Cleaning up Vanity UIDs from guild {guildName}", restGuild.Name);

                // get the list of allowed roles that should have vanity UID's from the Vanity Roles in the discord configuration.
                Dictionary<ulong, string> allowedRoleIds = _discordConfigService.GetValueOrDefault(nameof(DiscordConfiguration.VanityRoles), new Dictionary<ulong, string>());
                // display the list of allowed role ID's
                _logger.LogDebug($"Allowed role ids: {string.Join(", ", allowedRoleIds)}");

                // if there are not any allowed roles for vanity perks, output it.
                if (!allowedRoleIds.Any())
                {
                    _logger.LogInformation("No roles for command defined, no cleanup performed");
                }
                // otherwise, handle it.
                else
                {
                    // await the creation of a scope for this service
                    await using var scope = _services.CreateAsyncScope();
                    // fetch the gagspeakDatabaseConext from the database
                    await using (var db = scope.ServiceProvider.GetRequiredService<GagspeakDbContext>())
                    {
                        // search the database under the AccountClaimAuth table where a user is included...
                        var aliasedUsers = await db.AccountClaimAuth.Include("User")
                            // where the user is not null and the alias is not null (they have an alias)
                            .Where(c => c.User != null && !string.IsNullOrEmpty(c.User.Alias))
                            // arranged into a list
                            .ToListAsync().ConfigureAwait(false);

                        // then, for each of the aliased users in the list...
                        foreach (var accountClaimAuth in aliasedUsers)
                        {
                            // fetch the discord user they belong to by grabbing the discord ID from the accountClaimAuth
                            var discordUser = await restGuild.GetUserAsync(accountClaimAuth.DiscordId).ConfigureAwait(false);
                            _logger.LogInformation($"Checking User: {accountClaimAuth.DiscordId}, {accountClaimAuth.User.UID} " +
                            $"({accountClaimAuth.User.Alias}), User in Roles: {string.Join(", ", discordUser?.RoleIds ?? new List<ulong>())}");

                            // if the discord user no longer exists, or no longer has any of the allowed role ID's for these benifits....
                            if (discordUser == null || !discordUser.RoleIds.Any(u => allowedRoleIds.Keys.Contains(u)))
                            {
                                // we should clear the user's alias "and their other vanity benifits, but add those later)
                                _logger.LogInformation($"User {accountClaimAuth.User.UID} not in allowed roles, deleting alias");
                                accountClaimAuth.User.Alias = null;

                                // locate any secondary user's of the primary user this account belongs to, and clear the perks from these as well.
                                var secondaryUsers = await db.Auth.Include(u => u.User).Where(u => u.PrimaryUserUID == accountClaimAuth.User.UID).ToListAsync().ConfigureAwait(false);
                                foreach (var secondaryUser in secondaryUsers)
                                {
                                    _logger.LogDebug($"Secondary User {secondaryUser.User.UID} not in allowed roles, deleting alias");
                                    secondaryUser.User.Alias = null;
                                    // update the secondary user in the database
                                    db.Update(secondaryUser.User);
                                }
                                // update the primary user in the DB
                                db.Update(accountClaimAuth.User);
                            }

                            // await for the database to save changes
                            await db.SaveChangesAsync().ConfigureAwait(false);
                            // await a second before checking the next user
                            await Task.Delay(1000);
                            // keep in mind this only happens once on startup, so we shouldnt need to worry about it much.
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Something failed during checking vanity user UID's");
            }

            // log the completition, and execute it again in 12 hours.
            _logger.LogInformation("Vanity UID cleanup complete");
            await Task.Delay(TimeSpan.FromHours(12), _vanityUpdateCts.Token).ConfigureAwait(false);
        }
    }

    /// <summary> Updates the status of the bot at the interval </summary>
    private async Task UpdateStatusAsync()
    {
        _updateStatusCts = new();
        while (!_updateStatusCts.IsCancellationRequested)
        {
            // grab the endpoint from the connection multiplexer
            var endPoint = _connectionMultiplexer.GetEndPoints().First();
            // fetch the total number of online users connected to the redis server
            var onlineUsers = await _connectionMultiplexer.GetServer(endPoint).KeysAsync(pattern: "GagspeakHub:UID:*").CountAsync();

            // log the status
            _logger.LogTrace("Kinksters online: {onlineUsers}", onlineUsers);
            // change the activity
            await _discordClient.SetActivityAsync(new Game("with " + onlineUsers + " Kinksters")).ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(15)).ConfigureAwait(false);
        }
    }
}