namespace MpAgent.GitLab.MergeRequestReviewer.Entities.DTOs;

public sealed record GitLabMergeRequestReviewContext(
    GitLabMergeRequestInfo Info,
    IReadOnlyList<GitLabMergeRequestDiff> Diffs,
    string? EditorConfig
);

