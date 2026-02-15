using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MpAgent.GitLab.MergeRequestReviewer.Agents;

namespace MpAgent.AppState;

public partial class GitLabMergeRequestReviewerViewModel(GitLabReviewAgent agent) : ObservableObject
{
    // === STATE ===

    [ObservableProperty] private string mergeRequestUrl="";

    [ObservableProperty] private string personalAccessToken="";

    [ObservableProperty] private string reviewResult="";

    [ObservableProperty] private bool isLoading;

    // === COMMAND ===

    [RelayCommand]
    private async Task EvalAsync(CancellationToken cancellationToken)
    {
        try
        {
            ReviewResult = "Processing...";

            await agent.InitializeAsync(cancellationToken);

            var review = await agent.ReviewAsync(
                MergeRequestUrl,
                cancellationToken);

            ReviewResult = review;
        }
        catch (OperationCanceledException)
        {
            ReviewResult = "⚠️ Operation cancelled.";
        }
        catch (Exception ex)
        {
            ReviewResult = $"❌ Error: {ex.Message}";
        }
    }
}