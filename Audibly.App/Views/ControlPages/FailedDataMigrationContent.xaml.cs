using Audibly.App.Helpers;
using CommunityToolkit.Labs.WinUI.MarkdownTextBlock;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.Views.ControlPages;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class FailedDataMigrationContent : Page
{
    public MarkdownConfig MarkdownConfig; // = MarkdownConfig.Default;

    public FailedDataMigrationContent()
    {
        Title = "Data Migration Failed";
        Subtitle = $"Version {Constants.Version}";
        Text =
            "**Data migration failed. To re-attempt data migration, you can:**\n- Go to `Settings -> Advanced Settings -> View Audibly Export from Data Migration in File Explorer`\n- Import the `audibly_export.audibly` file using the `Import audiobooks from an Audibly export file (.audibly)` button\n\n**Unfortunately, if this also fails, you will have to re-import your audiobooks manually. Going forward, we'll never have to do this type of data migration again as I will be able to make database changes without needing to create a whole new database (this one is due to inexperience and poor planning on my part). I'm really sorry for the inconvenience.**\n\n**Please reach out to me via email at help@audibly.info if you would like help restoring your library and I will do my best to assist you. If you decide to reach out, please attach the following files (these will all be found in the same folder as the `audibly_export.audibly` file):**\n- `audibly_export.audibly`\n- `audibly.db.bak`\n- `Audibly.log`";

        MarkdownConfig = MarkdownConfig.Default;
        MarkdownConfig.Themes.InlineCodeFontSize = 36;


        InitializeComponent();
    }

    public string Title { get; set; }

    public string Text { get; set; }

    public string Subtitle { get; set; }
}