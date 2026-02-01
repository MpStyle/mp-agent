using Microsoft.Extensions.AI;
using MpAgent.GitLab.MergeRequestReviewer.Tools;

namespace MpAgent.GitLab.MergeRequestReviewer.Functions;

public static class GitLabAiFunctions
{
    public static AIFunction CreateMergeRequestFunction(GitLabMergeRequestTool tool)
    {
        return AIFunctionFactory.Create(
            async (string url) =>
            {
                var review = await tool.GetMergeRequestAsync(url);
                return new {
                    review.Info.Title,
                    review.Info.Description,
                    Diffs = review.Diffs.Select(d => new {
                        d.OldPath,
                        d.NewPath,
                        d.Diff
                    })
                };
            },
            name: "GetMergeRequestData",
            description: "Fetch GitLab merge request info + diffs."
        );
    }
}
