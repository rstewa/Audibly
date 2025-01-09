using CommunityToolkit.Labs.WinUI.MarkdownTextBlock;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.Views.ContentDialogs;

/// <summary>
///     A ContentDialog that displays a progress bar and a message.
/// </summary>
public sealed partial class ChangelogContentDialog : ContentDialog
{
    public MarkdownConfig MarkdownConfig = MarkdownConfig.Default;

    public ChangelogContentDialog()
    {
        InitializeComponent();
        ChangelogText = Changelog.Text;
    }

    public string ChangelogText { get; }

    public string Subtitle { get; set; } = string.Empty;
}