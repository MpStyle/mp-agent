using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using MpAgent.AppState;

namespace MpAgent.UI.Features.GitLabMergeRequestReviewer;

public partial class GitLabMergeRequestReviewer : ContentPage
{
    public GitLabMergeRequestReviewer(GitLabMergeRequestReviewerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}