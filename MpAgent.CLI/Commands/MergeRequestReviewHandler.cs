using System.CommandLine;
using MpAgent.GitLab.MergeRequestReviewer.Agents;

namespace MpAgent.CLI.Commands;

public class MergeRequestReviewHandler
{
    private const string Name = "merge-request-review";
    private const string UrlArgumentName = "url";
    
    private readonly GitLabReviewAgent agent;

    public Command Command { get; }

    public MergeRequestReviewHandler(GitLabReviewAgent agent)
    {
        this.agent = agent;
        this.Command= new Command(Name, "Perform code review on a GitLab MR")
        {
            new Argument<string>(UrlArgumentName)
            {
                Description = "The url of the GitLab Merge Request"
            }
        };
        this.Command.SetAction(this.Handler);
    }

    private async Task<int> Handler(ParseResult parseResult, CancellationToken cancellationToken)
    {
        try
        {
            await agent.InitializeAsync();
            var review = await agent.ReviewAsync(parseResult.GetRequiredValue<string>(UrlArgumentName), cancellationToken);
            Console.WriteLine("\n===== AI CODE REVIEW =====\n");
            Console.WriteLine(review);
            Console.WriteLine("\n==========================\n");
            return ErrorCode.Success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            return ErrorCode.CodeReviewError;
        }
    }
}