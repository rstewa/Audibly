// Author: rstewa · https://github.com/rstewa
// Created: 4/15/2024
// Updated: 6/1/2024

using Audibly.App.Helpers;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.Views.ControlPages;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ChangelogDialogContent : Page
{
    public string Title { get; set; }

    public string ChangelogText { get; set; }

    public string Subtitle { get; set; }

    public ChangelogDialogContent(string changelogText)
    {
        Title = "What's New?";
        Subtitle = $"Version {Constants.Version}";
        ChangelogText = changelogText;
        InitializeComponent();
    }
}