using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MpAgent.AppState;
using MpAgent.GitLab.MergeRequestReviewer.Agents;
using MpAgent.GitLab.MergeRequestReviewer.Entities;
using MpAgent.GitLab.MergeRequestReviewer.Tools;
using MpAgent.Translate.Agents;
using MpAgent.UI.BusinessModels;

namespace MpAgent.UI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
        
        builder.AddConfiguration();
        
        // View Models
        builder.Services.AddTransient<GitLabMergeRequestReviewerViewModel>();
        
        // HttpClientFactory for tools that need to make HTTP requests
        builder.Services.AddHttpClient();
        
        // Bind GitLab settings from configuration (appsettings.json / environment)
        builder.Services.Configure<GitLabSettingsOptions>(builder.Configuration.GetSection("GitLab"));
        
        // Also register the concrete GitLabSettingsOptions instance so it can be
        // injected directly where needed (in addition to IOptions<T>).
        builder.Services.AddSingleton(sp =>
            sp.GetRequiredService<IOptions<GitLabSettingsOptions>>().Value);

        // Register the tool resolving IOptions<GitLabSettingsOptions>
        builder.Services.AddSingleton(sp =>
            new GitLabMergeRequestTool(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
                sp.GetRequiredService<IOptions<GitLabSettingsOptions>>().Value
            ));

        // Agents
        builder.Services.AddSingleton<GitLabReviewAgent>();
        builder.Services.AddSingleton<TranslationAgent>();
        
#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}