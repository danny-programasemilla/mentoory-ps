using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Mentoory.Aspire.AppHost.AppHost;

public static class AppsettingsLoader
{
    public static IConfiguration LoadJsonConfiguration(bool includeDevelopment)
    {
        var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory) // Ensure correct path resolution
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false); // Base config

        if (includeDevelopment)
        {
            builder.AddJsonFile($"appsettings.Development.json", optional: true, reloadOnChange: false); // Environment override
        }

        return builder.Build();
    }

    public static string SerializeUserJsonConfiguration(bool includeDevelopment, out string hash)
    {
        var appSettingsConfig = LoadJsonConfiguration(includeDevelopment).AsEnumerable()
            .Where(kv => kv.Value is not null
                         && !kv.Key.StartsWith("ConnectionStrings")
                         && !kv.Key.StartsWith("Logging"))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        var json = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(appSettingsConfig));

        hash = Convert.ToHexString(SHA256.HashData(json));

        return Convert.ToBase64String(json);
    }
}
