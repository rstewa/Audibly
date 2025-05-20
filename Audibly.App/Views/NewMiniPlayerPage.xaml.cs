// Author: rstewa · https://github.com/rstewa
// Updated: 01/28/2025

using System;
using System.Diagnostics;
using Audibly.App.Extensions;
using Audibly.App.Helpers;
using Audibly.App.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
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

    // todo: fix the bug where this is getting triggered even when the user hasn't clicked the slider
    private async void NowPlayingBar_OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        var slider = sender as Slider;

        if (slider == null || slider.Value == 0) return;

        if (PlayerViewModel.NowPlaying?.CurrentChapter == null) return;

        PlayerViewModel.CurrentPosition =
            TimeSpan.FromMilliseconds(PlayerViewModel.NowPlaying.CurrentChapter.StartTime + slider.Value);

        await PlayerViewModel.NowPlaying.SaveAsync();
    }
    
    private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        var slider = sender as Slider;
        if (slider == null || !IsLoaded) return;

        PlayerViewModel.UpdateVolume(slider.Value);
    }

    private void PlaybackSpeedSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        var slider = sender as Slider;
        if (slider == null || !IsLoaded) return;

        PlayerViewModel.UpdatePlaybackSpeed(slider.Value);
    }
    
    private void VolumeButton_Click(object sender, RoutedEventArgs e)
    {
        // Show volume flyout with slider
        Flyout volumeFlyout = new Flyout();
    
        Grid grid = new Grid();
    
        Slider volumeSlider = new Slider
        {
            Orientation = Orientation.Vertical,
            Minimum = 0,
            Maximum = 1.0,
            SmallChange = 0.01,
            StepFrequency = 0.01,
            Value = PlayerViewModel.VolumeLevel,
            Height = 100
        };
        volumeSlider.ValueChanged += VolumeSlider_ValueChanged;
    
        grid.Children.Add(volumeSlider);
        volumeFlyout.Content = grid;
    
        volumeFlyout.ShowAt(sender as FrameworkElement);
    }

    private void PlaybackSpeedButton_Click(object sender, RoutedEventArgs e)
    {
        // Show playback speed flyout with slider
        Flyout speedFlyout = new Flyout();
    
        Grid grid = new Grid();
    
        Slider speedSlider = new Slider
        {
            Orientation = Orientation.Vertical,
            Minimum = 0.5,
            Maximum = 4.0,
            SmallChange = 0.05,
            StepFrequency = 0.05,
            Value = PlayerViewModel.PlaybackSpeed,
            Height = 100
        };
        speedSlider.ValueChanged += PlaybackSpeedSlider_ValueChanged;
    
        grid.Children.Add(speedSlider);
        speedFlyout.Content = grid;
    
        speedFlyout.ShowAt(sender as FrameworkElement);
    }

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
        WindowHelper.HideMiniPlayer();
    }
}