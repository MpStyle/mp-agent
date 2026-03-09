using GitHub.Copilot.SDK;

using MpAgent.GitLab.MergeRequestReviewer.Functions;
using MpAgent.GitLab.MergeRequestReviewer.Tools;

namespace MpAgent.GitLab.MergeRequestReviewer.Agents;


public sealed class GitLabReviewAgent(GitLabMergeRequestTool tool) : IAsyncDisposable
{
    private CopilotClient? copilotClient;
    private CopilotSession? session;

    public async Task InitializeAsync(Action<string> callback, Action onFinish, CancellationToken cancellationToken)
    {
        if (copilotClient != null)
            return;

        copilotClient = new CopilotClient();
        await copilotClient.StartAsync(cancellationToken);

        var gitLabFn = GitLabAiFunctions.CreateMergeRequestFunction(tool);

        this.session = await copilotClient.CreateSessionAsync(new()
        {
            OnPermissionRequest = PermissionHandler.ApproveAll,
            Tools = [gitLabFn],
            CustomAgents =
            [
                new() {
                    Name = "GitLabReviewAgent",
                    Description = "Agent specialized in reviewing GitLab merge requests with a focus on C# and TypeScript code quality and style.",
                    Tools = [gitLabFn.Name],
                    Prompt = """
            You are a senior software engineer specialized in C# and TypeScript code reviews.
            
            You will receive:
            - GitLab merge request metadata
            - Code diffs
            - The content of a .editorconfig file (if present)
            
            RULES:
            
            1. If a .editorconfig file is present:
               - Treat it as the PRIMARY source of coding rules
               - Do NOT invent style rules that conflict with it
            
            2. Review ONLY changed lines from the diff.
            
            3. For each issue found:
               - Identify the file path
               - Identify the exact line or line range
               - Quote the relevant code snippet
               - Classify severity using these levels:
                 Critical = mandatory fix, high risk
                 High = strongly recommended fix
                 Medium = important but only recommended
                 Low = optional suggestion
            
            4. Language-specific rules:
               - For C#: follow .NET conventions, async correctness, nullability, performance
               - For TypeScript: enforce type safety, strictness, async handling
            
            5. Output MUST be structured and deterministic.
            
            OUTPUT FORMAT (MANDATORY):
            
            For each file:
            
            File: <path>
            
            - [Critical]
              Location: lines X-Y
              Rule violated: <editorconfig rule or best practice>
              Explanation: <why>
              Suggested fix: <what to change>
            
            - [High]
              Location: lines X-Y
              ...
            
            - [Medium]
              Location: lines X-Y
              ...
            
            - [Low]
              Location: lines X-Y
              ...
            
            """
                }
            ]
        }, cancellationToken);

        session.On(ev =>
        {
            if (ev is AssistantMessageEvent assistantMessageEvent)
            {
                callback(assistantMessageEvent.Data.Content);
            }
            if (ev is AssistantMessageDeltaEvent deltaEvent)
            {
                callback(deltaEvent.Data.DeltaContent);
            }
            if (ev is SessionIdleEvent)
            {
                onFinish();
            }
        });
    }

    public async Task ReviewAsync(string mergeRequestUrl, CancellationToken cancellationToken)
    {
        if (session == null)
            throw new InvalidOperationException("Session not initialized");

        var prompt =
            $"Review the following GitLab merge request:\n{mergeRequestUrl}";

        await session.SendAsync(new MessageOptions { Prompt = prompt }, cancellationToken: cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (copilotClient != null)
        {
            await copilotClient.DisposeAsync();
            copilotClient = null;
        }
    }
}
