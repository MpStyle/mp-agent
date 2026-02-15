using GitHub.Copilot.SDK;
using Microsoft.Agents.AI;
using MpAgent.GitLab.MergeRequestReviewer.Functions;
using MpAgent.GitLab.MergeRequestReviewer.Tools;

namespace MpAgent.GitLab.MergeRequestReviewer.Agents;


public sealed class GitLabReviewAgent(GitLabMergeRequestTool tool) : IAsyncDisposable
{
    private CopilotClient? copilotClient;
    private AIAgent? agent;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (copilotClient != null)
            return;

        copilotClient = new CopilotClient();
        await copilotClient.StartAsync(cancellationToken);

        var gitLabFn = GitLabAiFunctions.CreateMergeRequestFunction(tool);

        agent = copilotClient.AsAIAgent(
            instructions:
            """
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
               - Classify severity using these colors:
                 🔴 RED   = mandatory fix, strongly recommended
                 🟠 ORANGE = important but only recommended
                 🟢 GREEN = optional suggestion
            
            4. Language-specific rules:
               - For C#: follow .NET conventions, async correctness, nullability, performance
               - For TypeScript: enforce type safety, strictness, async handling
            
            5. Output MUST be structured and deterministic.
            
            OUTPUT FORMAT (MANDATORY):
            
            For each file:
            
            File: <path>
            
            - 🔴 [RED]
              Location: lines X-Y
              Rule violated: <editorconfig rule or best practice>
              Explanation: <why>
              Suggested fix: <what to change>
            
            - 🟠 [ORANGE]
              Location: lines X-Y
              ...
            
            - 🟢 [GREEN]
              Location: lines X-Y
              ...
            
            """,
            tools: [gitLabFn]
        );
    }

    public async Task<string> ReviewAsync(string mergeRequestUrl, CancellationToken cancellationToken = default)
    {
        if (agent == null)
            throw new InvalidOperationException("Agent not initialized");

        var prompt =
            $"Review the following GitLab merge request:\n{mergeRequestUrl}";

        var response = await agent.RunAsync(prompt, cancellationToken: cancellationToken);
        return response.Text;
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
