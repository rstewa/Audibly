// Author: rstewa · https://github.com/rstewa
// Updated: 01/28/2025

using Audibly.App.Helpers;
using Audibly.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class PlayerPage : Page
{
    public PlayerPage()
    {
        InitializeComponent();

        // Set the title bar for the current view
        App.Window.ExtendsContentIntoTitleBar = true;
        App.Window.SetTitleBar(NowPlayingAppTitleBar);
    }

    /// <summary>
    ///     Gets the app-wide PlayerViewModel instance.
    /// </summary>
    public PlayerViewModel PlayerViewModel => App.PlayerViewModel;

    private void BackButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!Frame.CanGoBack) return;

        Frame.GoBack();
        PlayerViewModel.IsPlayerFullScreen = false;
        PlayerViewModel.MaximizeMinimizeGlyph = Constants.MaximizeGlyph;
    }
}