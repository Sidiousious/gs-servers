﻿using GagspeakAPI.Data.Enum;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Dto.Connection;
using GagspeakAPI.Dto.Permissions;
using GagspeakAPI.Dto.UserPair;
using GagspeakAPI.Dto.Toybox;
using GagspeakAPI.Dto.IPC;

namespace GagspeakServer.Hubs
{
    /// <summary> This section of the partial class for GagspeakHub contains the client stubs.
    /// <para> Client stubs are the functions that the server can call upon from its connected clients.</para>
    /// <para>
    /// This means that the clients should never be able to call these functions on the server.
    /// If they do try, we will throw an exception for each of these methods.
    /// </para>
    /// </summary>
    public partial class GagspeakHub
    {
        public Task Client_ReceiveServerMessage(MessageSeverity messageSeverity, string message) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_ReceiveHardReconnectMessage(MessageSeverity messageSeverity, string message, ServerState state) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UpdateSystemInfo(SystemInfoDto systemInfo) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");

        public Task Client_UserApplyMoodlesByGuid(ApplyMoodlesByGuidDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserApplyMoodlesByStatus(ApplyMoodlesByStatusDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserRemoveMoodles(RemoveMoodlesDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserClearMoodles(UserDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");

        public Task Client_UserAddClientPair(UserPairDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserRemoveClientPair(UserDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UpdateUserIndividualPairStatusDto(UserIndividualPairStatusDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        
        public Task Client_UserUpdateSelfAllGlobalPerms(UserAllGlobalPermChangeDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserUpdateSelfAllUniquePerms(UserPairUpdateAllUniqueDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserUpdateSelfPairPermsGlobal(UserGlobalPermChangeDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserUpdateSelfPairPerms(UserPairPermChangeDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserUpdateSelfPairPermAccess(UserPairAccessChangeDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserUpdateOtherAllPairPerms(UserPairUpdateAllPermsDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserUpdateOtherAllGlobalPerms(UserAllGlobalPermChangeDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
		public Task Client_UserUpdateOtherAllUniquePerms(UserPairUpdateAllUniqueDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserUpdateOtherPairPermsGlobal(UserGlobalPermChangeDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
		public Task Client_UserUpdateOtherPairPerms(UserPairPermChangeDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserUpdateOtherPairPermAccess(UserPairAccessChangeDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        
        public Task Client_UserReceiveCharacterDataComposite(OnlineUserCompositeDataDto dataDto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserReceiveOwnDataIpc(OnlineUserCharaIpcDataDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserReceiveOtherDataIpc(OnlineUserCharaIpcDataDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserReceiveOwnDataAppearance(OnlineUserCharaAppearanceDataDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserReceiveOtherDataAppearance(OnlineUserCharaAppearanceDataDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserReceiveOwnDataWardrobe(OnlineUserCharaWardrobeDataDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserReceiveOtherDataWardrobe(OnlineUserCharaWardrobeDataDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserReceiveOwnDataAlias(OnlineUserCharaAliasDataDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserReceiveOtherDataAlias(OnlineUserCharaAliasDataDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserReceiveOwnDataToybox(OnlineUserCharaToyboxDataDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserReceiveOtherDataToybox(OnlineUserCharaToyboxDataDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_GlobalChatMessage(GlobalChatMessageDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserSendOffline(UserDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserSendOnline(OnlineUserIdentDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserUpdateProfile(UserDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_DisplayVerificationPopup(VerificationDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
    }
}