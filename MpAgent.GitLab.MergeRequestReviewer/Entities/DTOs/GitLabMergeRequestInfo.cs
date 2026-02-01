namespace MpAgent.GitLab.MergeRequestReviewer.Entities;

public sealed record GitLabMergeRequestInfo(
    int Id,
    int Iid,
    string Title,
    string Description,
    string Author,
    string SourceBranch,
    string TargetBranch,
    string State,
    string WebUrl
);