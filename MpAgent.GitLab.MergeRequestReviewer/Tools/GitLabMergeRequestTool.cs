using MpAgent.GitLab.MergeRequestReviewer.Entities;
using MpAgent.GitLab.MergeRequestReviewer.Entities.DTOs;

using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MpAgent.GitLab.MergeRequestReviewer.Tools;

public sealed partial class GitLabMergeRequestTool
{
    private readonly HttpClient httpClient;
    private readonly GitLabSettingsOptions settingsOptions;

    public GitLabMergeRequestTool(
        HttpClient httpClient, GitLabSettingsOptions settingsOptions)
    {
        this.httpClient = httpClient;
        this.settingsOptions = settingsOptions;

        this.httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", settingsOptions.PersonalAccessToken);
    }

    public async Task<GitLabMergeRequestReviewContext> GetMergeRequestAsync(string mergeRequestUrl)
    {
        Console.WriteLine($"Parsing merge request URL: {mergeRequestUrl}...");

        var parsed = ParseMergeRequestUrl(mergeRequestUrl);

        Console.WriteLine($"Fetching merge request info for {parsed.Group}/{parsed.Project}!{parsed.MergeRequestIid}...");

        var projectId = this.GetProject(parsed.Group, parsed.Project);
        if (projectId == null)
        {
            Console.WriteLine("Invalid project or group. Please check the URL and try again.");
            throw new ArgumentException("Project not found", nameof(mergeRequestUrl));
        }

        Console.WriteLine($"Project ID: {projectId.Value}. Fetching merge request details...");

        var info = await this.GetMergeRequestInfoAsync(projectId.Value, parsed.MergeRequestIid);

        Console.WriteLine($"Project ID: {projectId.Value}. Fetching merge request diffs...");

        var diffs = await this.GetMergeRequestDiffsAsync(projectId.Value, parsed.MergeRequestIid);

        var editorConfig = await this.GetEditorConfigAsync(
            projectId.Value,
            info.TargetBranch
        );

        if (editorConfig == null)
        {
            Console.WriteLine("No .editorconfig found in the repository. Default settings will be used.");
        }

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
        {
            var srcUrl =
                $"{settingsOptions.BaseUrl}/api/v4/projects/{projectId}/repository/files/Src%2F.editorconfig/raw?ref={refName}";

            using var srcResponse = await httpClient.GetAsync(srcUrl);

            if (srcResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            srcResponse.EnsureSuccessStatusCode();
            return await srcResponse.Content.ReadAsStringAsync();
        }

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

    /// <summary>
    /// Retrieves detailed information about a specific GitLab merge request asynchronously.
    /// </summary>
    /// <remarks>The method performs an HTTP request to the GitLab API and requires valid project and merge
    /// request IDs. The operation will fail if the merge request does not exist or if the API is unreachable.</remarks>
    /// <param name="projectId">The unique identifier of the GitLab project containing the merge request.</param>
    /// <param name="mrIid">The internal ID of the merge request within the specified project.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see
    /// cref="GitLabMergeRequestInfo"/> object with information about the merge request.</returns>
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

    /// <summary>
    /// Retrieves the list of file-level changes for a specific merge request.
    /// </summary>
    /// <remarks>
    /// This method calls the GitLab API `changes` endpoint and maps each returned change
    /// into a <see cref="GitLabMergeRequestDiff"/> instance. The request will fail if
    /// the merge request does not exist or the API is unreachable.
    /// </remarks>
    /// <param name="projectId">The unique identifier of the GitLab project containing the merge request.</param>
    /// <param name="mrIid">The internal ID of the merge request within the specified project.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a read-only list of
    /// <see cref="GitLabMergeRequestDiff"/> instances describing the changed files and diff content.
    /// </returns>
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

    [GeneratedRegex(@"https?:\/\/.+?\/(?<group>.+?)\/(?<project>[^\/]+)\/-\/merge_requests\/(?<iid>\d+)", RegexOptions.Compiled)]
    private static partial Regex GitLabMergeRequestRegex();
}
