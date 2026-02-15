using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace MpAgent.UI.BusinessModels;

public static class AppSettingsLoader
{
    public static MauiAppBuilder AddConfiguration(this MauiAppBuilder builder)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var configBuilder = new ConfigurationBuilder();

        using var appsettingsStream = assembly.GetManifestResourceStream("MpAgent.UI.appsettings.json");
        if (appsettingsStream is null)
        {
            throw new FileNotFoundException("Could not find appsettings.json");
        }

        configBuilder.AddJsonStream(appsettingsStream);

        using var appsettingsDevelopmentStream =
            assembly.GetManifestResourceStream("MpAgent.UI.appsettings.development.json");
        if (appsettingsDevelopmentStream is not null)
        {
            configBuilder.AddJsonStream(appsettingsDevelopmentStream);
        }

        var configuration = configBuilder.Build();
        builder.Configuration.AddConfiguration(configuration);

        return builder;
    }
}