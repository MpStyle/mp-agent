using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using MpAgent.GitLab.MergeRequestReviewer.Entities;
using MpAgent.GitLab.MergeRequestReviewer.Entities.DTOs;

namespace MpAgent.GitLab.MergeRequestReviewer.Tools;

public sealed partial class GitLabMergeRequestTool
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
        var projectId = this.GetProject(parsed.Group, parsed.Project);
        if (projectId == null)
        {
            Console.WriteLine("Invalid project or group. Please check the URL and try again.");
            throw new ArgumentException("Project not found", nameof(mergeRequestUrl));
        }

        var info = await this.GetMergeRequestInfoAsync(projectId.Value, parsed.MergeRequestIid);
        var diffs = await this.GetMergeRequestDiffsAsync(projectId.Value, parsed.MergeRequestIid);

        var editorConfig = await this.GetEditorConfigAsync(
            projectId.Value,
            info.TargetBranch
        );

        return new GitLabMergeRequestReviewContext(info, diffs, editorConfig);
    }


    // -----------------------------
    // GitLab API calls
    // -----------------------------
    
    private async Task<string?> GetEditorConfigAsync(int projectId, string refName)
    {
        // refName = target branch (es. main)
        var url =
            $"{settingsOptions.BaseUrl}/api/v4/projects/{projectId}/repository/files/.editorconfig/raw?ref={refName}";

        using var response = await httpClient.GetAsync(url);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="group"></param>
    /// <param name="project"></param>
    /// <returns>Project id</returns>
    private int? GetProject(string group, string project)
    {
        var url = $"{settingsOptions.BaseUrl}/api/v4/projects/{Uri.EscapeDataString($"{group}/{project}")}";
        using var response = httpClient.GetAsync(url).Result;
        response.EnsureSuccessStatusCode();
        using var stream = response.Content.ReadAsStreamAsync().Result;
        using var json = JsonDocument.ParseAsync(stream).Result;
        var projectId = json.RootElement.GetProperty("id").GetInt32();
        return projectId;
    }

    private async Task<GitLabMergeRequestInfo> GetMergeRequestInfoAsync(
        int projectId,
        int mrIid)
    {
        var url = $"{settingsOptions.BaseUrl}/api/v4/projects/{projectId}/merge_requests/{mrIid}";
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
        int projectId,
        int mrIid)
    {
        var url = $"{settingsOptions.BaseUrl}/api/v4/projects/{projectId}/merge_requests/{mrIid}/changes";
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

    /// <summary>
    /// Parses a GitLab merge request URL and extracts the group, project, and merge request IID.
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private static (string Group, string Project, int MergeRequestIid) ParseMergeRequestUrl(string url)
    {
        // example:
        // https://gitlab.com/gitlab-org/gitlab-foss/-/merge_requests/33101
        var regex = GitLabMergeRequestRegex();

        var match = regex.Match(url);
        if (!match.Success)
            throw new ArgumentException("Invalid GitLab merge request URL", nameof(url));

        return (
            match.Groups["group"].Value,
            match.Groups["project"].Value,
            int.Parse(match.Groups["iid"].Value)
        );
    }

    [GeneratedRegex(@"https?:\/\/.+?\/(?<group>[^\/]+)\/(?<project>[^\/]+)\/-\/merge_requests\/(?<iid>\d+)", RegexOptions.Compiled)]
    private static partial Regex GitLabMergeRequestRegex();
}
