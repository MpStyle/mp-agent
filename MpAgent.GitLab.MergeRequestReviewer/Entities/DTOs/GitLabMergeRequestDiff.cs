namespace MpAgent.GitLab.MergeRequestReviewer.Entities;

public sealed record GitLabMergeRequestDiff(
    string OldPath,
    string NewPath,
    bool NewFile,
    bool DeletedFile,
    bool RenamedFile,
    string Diff
);