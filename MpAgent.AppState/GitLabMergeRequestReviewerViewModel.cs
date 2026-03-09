using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MpAgent.GitLab.MergeRequestReviewer.Agents;

namespace MpAgent.AppState;

public partial class GitLabMergeRequestReviewerViewModel(GitLabReviewAgent agent) : ObservableObject
{
    #region State
    [ObservableProperty] private string mergeRequestUrl = "";

    [ObservableProperty] private string reviewResult = "";

    [ObservableProperty] private bool isLoading = false;

    [ObservableProperty] private bool isEvalButtonEnabled = false;
    [ObservableProperty] private bool isMergeRequestUrlEnabled = true;
    #endregion

    #region Commands
    [RelayCommand]
    private async Task EvalAsync(CancellationToken cancellationToken)
    {
        try
        {
            this.IsLoading = true;

            this.ReviewResult = string.Empty; // "Processing...";

            await agent.InitializeAsync(s =>
                {
                    this.ReviewResult += s + "\n";
                }, () =>
                {
                    this.IsLoading = false;
                }, cancellationToken);

            await agent.ReviewAsync(
                MergeRequestUrl,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            this.ReviewResult = "⚠️ Operation cancelled.";
            this.IsLoading = false;
        }
        catch (Exception ex)
        {
            this.ReviewResult = $"❌ Error: {ex.Message}";
            this.IsLoading = false;
        }
    }
    #endregion

    #region Handlers
    partial void OnMergeRequestUrlChanged(string value)
    {
        this.IsEvalButtonEnabled = !string.IsNullOrEmpty(value.Trim());
    }

    partial void OnIsLoadingChanged(bool oldValue, bool newValue)
    {
        this.IsEvalButtonEnabled = !newValue;
        this.IsMergeRequestUrlEnabled = !newValue;
    }
    #endregion
}