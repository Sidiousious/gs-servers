using System.Reflection;
using System.Text;
using System.Text.Json;

namespace GagspeakServer.Utils.Configuration;

public class GagspeakConfigBase : IGagspeakConfig
{
    public int DbContextPoolSize { get; set; } = 100;   // The size of the DbContext pool
    public string Jwt { get; set; } = string.Empty;     // the JWT secret
    public Uri MainServerAddress { get; set; }          // The address of the main server
    public string RedisConnectionString { get; set; } = string.Empty; // The connection string for the redis server ???? (not sure why i have this but it breaks without it lmao)

    // get the value of the key
    public T GetValue<T>(string key)
    {
        var prop = GetType().GetProperty(key);
        if (prop == null) throw new KeyNotFoundException(key);
        if (prop.PropertyType != typeof(T)) throw new ArgumentException($"Requested {key} with T:{typeof(T)}, where {key} is {prop.PropertyType}");
        return (T)prop.GetValue(this);
    }

    // basic getvalue or default type definition
    public T GetValueOrDefault<T>(string key, T defaultValue)
    {
        var prop = GetType().GetProperty(key);
        if (prop.PropertyType != typeof(T)) throw new ArgumentException($"Requested {key} with T:{typeof(T)}, where {key} is {prop.PropertyType}");
        if (prop == null) return defaultValue;
        return (T)prop.GetValue(this);
    }

    // serialize the damn value AAAAAAAAAAAAA
    public string SerializeValue(string key, string defaultValue)
    {
        var prop = GetType().GetProperty(key);
        if (prop == null) return defaultValue;
        if (prop.GetCustomAttribute<RemoteConfigAttribute>() == null) return defaultValue;
        return JsonSerializer.Serialize(prop.GetValue(this), prop.PropertyType);
    }

    // get a return string of the information.
    public override string ToString()
    {
        StringBuilder sb = new();
        sb.AppendLine(base.ToString());
        sb.AppendLine($"{nameof(MainServerAddress)} => {MainServerAddress}");
        sb.AppendLine($"{nameof(DbContextPoolSize)} => {DbContextPoolSize}");
        return sb.ToString();
    }
}