using Microsoft.Extensions.Configuration;

namespace Nexus.Permissions.Export.Properties;
public class Settings
{
    private static readonly string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
    private static readonly string settingsDevelopPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.Development.json");
    public OAuthSettings OAuth { get; set; }
    public static Settings LoadSettings()
    {
        // Load settings
        IConfiguration config = new ConfigurationBuilder()
            // appsettings.json is required
            .AddJsonFile(settingsPath)
            .Build();

        return config.Get<Settings>();
    }
}

public class OAuthSettings
{
    public string ClientId { get; set; }
    public string RedirectUrl { get; set; }
    public string[] GraphUserScopes { get; set; }
}