using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using MpAgent.GitLab.MergeRequestReviewer.Entities;
using MpAgent.GitLab.MergeRequestReviewer.Entities.DTOs;

namespace MpAgent.GitLab.MergeRequestReviewer.Tools;

public sealed class GitLabMergeRequestTool
{
    private readonly HttpClient httpClient;
    private readonly GitLabSettingsOptions settingsOptions;

    public GitLabMergeRequestTool(
        HttpClient httpClient,GitLabSettingsOptions settingsOptions)
    {
        this.httpClient = httpClient;
        this.settingsOptions = settingsOptions;

        this.httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", settingsOptions.PersonalAccessToken);
    }

    public async Task<GitLabMergeRequestReviewContext> GetMergeRequestAsync(string mergeRequestUrl)
    {
        var parsed = ParseMergeRequestUrl(mergeRequestUrl);

        var info = await GetMergeRequestInfoAsync(parsed.ProjectPath, parsed.MergeRequestIid);
        var diffs = await GetMergeRequestDiffsAsync(parsed.ProjectPath, parsed.MergeRequestIid);

        var editorConfig = await GetEditorConfigAsync(
            parsed.ProjectPath,
            info.TargetBranch
        );

        return new GitLabMergeRequestReviewContext(info, diffs, editorConfig);
    }


    // -----------------------------
    // GitLab API calls
    // -----------------------------
    
    private async Task<string?> GetEditorConfigAsync(string projectPath, string refName)
    {
        // refName = target branch (es. main)
        var url =
            $"{settingsOptions.BaseUrl}/api/v4/projects/{Uri.EscapeDataString(projectPath)}/repository/files/.editorconfig/raw?ref={refName}";

        using var response = await httpClient.GetAsync(url);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }


    private async Task<GitLabMergeRequestInfo> GetMergeRequestInfoAsync(
        string projectPath,
        int mrIid)
    {
        var url = $"{settingsOptions.BaseUrl}/api/v4/projects/{Uri.EscapeDataString(projectPath)}/merge_requests/{mrIid}";
        using var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(stream);
        var root = json.RootElement;

        return new GitLabMergeRequestInfo(
            root.GetProperty("id").GetInt32(),
            root.GetProperty("iid").GetInt32(),
            root.GetProperty("title").GetString()!,
            root.GetProperty("description").GetString() ?? string.Empty,
            root.GetProperty("author").GetProperty("name").GetString()!,
            root.GetProperty("source_branch").GetString()!,
            root.GetProperty("target_branch").GetString()!,
            root.GetProperty("state").GetString()!,
            root.GetProperty("web_url").GetString()!
        );
    }

    private async Task<IReadOnlyList<GitLabMergeRequestDiff>> GetMergeRequestDiffsAsync(
        string projectPath,
        int mrIid)
    {
        var url = $"{settingsOptions.BaseUrl}/api/v4/projects/{Uri.EscapeDataString(projectPath)}/merge_requests/{mrIid}/changes";
        using var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(stream);

        var changes = json.RootElement.GetProperty("changes");
        var result = new List<GitLabMergeRequestDiff>();

        foreach (var change in changes.EnumerateArray())
        {
            result.Add(new GitLabMergeRequestDiff(
                change.GetProperty("old_path").GetString()!,
                change.GetProperty("new_path").GetString()!,
                change.GetProperty("new_file").GetBoolean(),
                change.GetProperty("deleted_file").GetBoolean(),
                change.GetProperty("renamed_file").GetBoolean(),
                change.GetProperty("diff").GetString()!
            ));
        }

        return result;
    }

    // -----------------------------
    // URL parsing
    // -----------------------------

    private static (string ProjectPath, int MergeRequestIid) ParseMergeRequestUrl(string url)
    {
        // esempio:
        // https://gitlab.company.local/group/project/-/merge_requests/42
        var regex = new Regex(
            @"https?:\/\/.+\/(?<project>.+?)\/-\/merge_requests\/(?<iid>\d+)",
            RegexOptions.Compiled);

        var match = regex.Match(url);
        if (!match.Success)
            throw new ArgumentException("Invalid GitLab merge request URL");

        return (
            match.Groups["project"].Value,
            int.Parse(match.Groups["iid"].Value)
        );
    }
}
