// Author: rstewa · https://github.com/rstewa
// Created: 4/11/2024
// Updated: 4/11/2024

using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.Views.ControlPages;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ChangelogDialogContent : Page
{
    public string Title { get; set; }

    public string ChangelogText { get; set; }
    
    public ChangelogDialogContent(string title, string changelogText)
    {
        Title = title;
        ChangelogText = changelogText;
        InitializeComponent();
    }
}