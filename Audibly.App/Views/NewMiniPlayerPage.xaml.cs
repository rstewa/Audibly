using System;
using Audibly.App.Extensions;
using Audibly.App.Helpers;
using Audibly.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Audibly.App.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class NewMiniPlayerPage : Page
{
    public NewMiniPlayerPage()
    {
        InitializeComponent();
    }

    /// <summary>
    ///     Gets the app-wide ViewModel instance.
    /// </summary>
    public MainViewModel ViewModel => App.ViewModel;

    /// <summary>
    ///     Gets the app-wide PlayerViewModel instance.
    /// </summary>
    public PlayerViewModel PlayerViewModel => App.PlayerViewModel;

    private async void NowPlayingBar_OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        var slider = sender as Slider;
        if (slider != null && slider.Value != 0)
        {
            PlayerViewModel.CurrentPosition =
                TimeSpan.FromMilliseconds(PlayerViewModel.NowPlaying.CurrentChapter.StartTime + slider.Value);

            await PlayerViewModel.NowPlaying.SaveAsync();
        }
    }

    private const string _pinnedIcon = "&#xE840;";
    private const string _unpinnedIcon = "&#xE77A;";
    
    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        var window = WindowHelper.GetMiniPlayerWindow();
        if (window == null) return;
        
        PinButton.Visibility = Visibility.Collapsed;
        UnpinButton.Visibility = Visibility.Visible;
        
        window.SetWindowDraggable(false);
        window.SetWindowAlwaysOnTop(true);
    }

    private void UnpinButton_Click(object sender, RoutedEventArgs e)
    {
        var window = WindowHelper.GetMiniPlayerWindow();
        if (window == null) return;
        
        UnpinButton.Visibility = Visibility.Collapsed;
        PinButton.Visibility = Visibility.Visible;
        
        window.SetWindowDraggable(true);
        window.SetWindowAlwaysOnTop(false);
    }

    private void BackToLibraryButton_Click(object sender, RoutedEventArgs e)
    {
        WindowHelper.RestoreMainWindow();
        // WindowHelper.HideMiniPlayer();
        WindowHelper.CloseMiniPlayer();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        App.Current.Exit();
    }
}