// Author: rstewa · https://github.com/rstewa
// Updated: 01/28/2025

using System;
using System.ComponentModel;
using Audibly.App.Helpers;
using Audibly.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

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

        Loaded += OnPageLoaded;
        Unloaded += OnPageUnloaded;
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        TranscriptVm.PropertyChanged += OnTranscriptVmPropertyChanged;
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        TranscriptVm.PropertyChanged -= OnTranscriptVmPropertyChanged;
    }

    private void OnTranscriptVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TranscriptViewModel.IsPaneOpen)) RefreshCoverBrush();
    }

    /// <summary>
    ///     An ImageBrush discards its content while its element is collapsed and does not
    ///     repaint when it becomes visible again — re-set the source on every pane toggle.
    /// </summary>
    private void RefreshCoverBrush()
    {
        var path = PlayerViewModel.NowPlaying?.CoverImagePath;
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            var source = new BitmapImage(new Uri(path));
            if (TranscriptVm.IsPaneOpen) SmallCoverBrush.ImageSource = source;
            else BigCoverBrush.ImageSource = source;
        }
        catch (Exception ex)
        {
            App.ViewModel.LoggingService.LogError(ex, false);
        }
    }

    /// <summary>
    ///     Gets the app-wide PlayerViewModel instance.
    /// </summary>
    public PlayerViewModel PlayerViewModel => App.PlayerViewModel;

    /// <summary>
    ///     Gets the app-wide transcript (read-along) view model instance.
    /// </summary>
    public TranscriptViewModel TranscriptVm => App.TranscriptVm;

    private void BackButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!Frame.CanGoBack) return;

        Frame.GoBack();
        PlayerViewModel.IsPlayerFullScreen = false;
        PlayerViewModel.MaximizeMinimizeGlyph = Constants.MaximizeGlyph;
    }

    private void TranscriptToggle_OnClick(object sender, RoutedEventArgs e)
    {
        TranscriptVm.TogglePane();
    }
}