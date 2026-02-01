namespace MpAgent.GitLab.MergeRequestReviewer.Entities;

public record GitLabSettingsOptions
{
    public string BaseUrl
    {
        get;

        init => field = value.TrimEnd('/');
    } = string.Empty;

    public string PersonalAccessToken { get; init; } = string.Empty;
}