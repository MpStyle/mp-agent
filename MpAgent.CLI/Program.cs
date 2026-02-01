using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MpAgent.CLI.Commands;
using MpAgent.GitLab.MergeRequestReviewer.Agents;
using MpAgent.GitLab.MergeRequestReviewer.Entities;
using MpAgent.GitLab.MergeRequestReviewer.Tools;

namespace MpAgent.CLI;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        // Host and DI setup
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddHttpClient();

                // Bind GitLab settings from configuration (appsettings.json / environment)
                services.Configure<GitLabSettingsOptions>(
                    context.Configuration.GetSection("GitLab"));

                // Also register the concrete GitLabSettingsOptions instance so it can be
                // injected directly where needed (in addition to IOptions<T>).
                services.AddSingleton(sp =>
                    sp.GetRequiredService<IOptions<GitLabSettingsOptions>>().Value);

                // Register the tool resolving IOptions<GitLabSettingsOptions>
                services.AddSingleton(sp =>
                    new GitLabMergeRequestTool(
                        sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
                        sp.GetRequiredService<IOptions<GitLabSettingsOptions>>().Value
                    ));

                // Commands
                services.AddSingleton<MergeRequestReviewHandler>();

                // Agents
                services.AddSingleton<GitLabReviewAgent>();
            })
            .Build();

        // Commands setup
        var mergeRequestReviewCommand = host.Services.GetRequiredService<MergeRequestReviewHandler>();

        var rootCommand = new RootCommand("Multi-purpose AI agent CLI")
        {
            mergeRequestReviewCommand.Command
        };

        var result = rootCommand.Parse(args);
        await result.InvokeAsync();
    }
}