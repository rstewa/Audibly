// Author: rstewa · https://github.com/rstewa
// Updated: 06/03/2025

using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;
using Audibly.App.Extensions;
using Audibly.App.Helpers;
using Audibly.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Constants = Audibly.App.Helpers.Constants;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace Audibly.App.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class NewMiniPlayerPage : Page
{
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private CancellationTokenSource? _playbackSpeedFlyoutCts;

    public NewMiniPlayerPage()
    {
        InitializeComponent();
        KeyDown += NewMiniPlayerPage_KeyDown;

        // need this to show the slider when the user increases or decreases the speed with the keyboard shortcuts
        PlaybackSpeedSliderFlyout.Opened += (s, e) => { PlaybackSpeedSlider.Focus(FocusState.Programmatic); };
    }

    /// <summary>
    ///     Gets the app-wide ViewModel instance.
    /// </summary>
    public MainViewModel ViewModel => App.ViewModel;

    /// <summary>
    ///     Gets the app-wide PlayerViewModel instance.
    /// </summary>
    public PlayerViewModel PlayerViewModel => App.PlayerViewModel;

    private void NewMiniPlayerPage_KeyDown(object sender, KeyRoutedEventArgs args)
    {
        var key = args.Key;
        if (key == (VirtualKey)219) // Open bracket '['
        {
            HandleSpeedDecrease();
            ClosePlaybackSpeedFlyout();
            args.Handled = true;
        }
        else if (key == (VirtualKey)221) // Close bracket ']'
        {
            HandleSpeedIncrease();
            ClosePlaybackSpeedFlyout();
            args.Handled = true;
        }
        else if (key == (VirtualKey)0xDC) // Backslash '\'
        {
            ResetPlaybackSpeed();
            ClosePlaybackSpeedFlyout();
            args.Handled = true;
        }
    }

    private void ClosePlaybackSpeedFlyout()
    {
        // Cancel any previous close operation
        _playbackSpeedFlyoutCts?.Cancel();
        _playbackSpeedFlyoutCts = new CancellationTokenSource();
        var token = _playbackSpeedFlyoutCts.Token;

        _dispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                await Task.Delay(2000, token);
                if (PlaybackSpeedSliderFlyout.IsOpen)
                    PlaybackSpeedSliderFlyout.Hide();
            }
            catch (TaskCanceledException)
            {
                // Ignore cancellation
            }
        });
    }

    private void ResetPlaybackSpeed()
    {
        PlaybackSpeedSliderFlyout.ShowAt(PlaybackSpeedButton);
        PlaybackSpeedSlider.Value = Constants.PlaybackSpeedDefault;
    }

    private void HandleSpeedIncrease()
    {
        PlaybackSpeedSliderFlyout.ShowAt(PlaybackSpeedButton);
        var newValue = PlaybackSpeedSlider.Value + Constants.PlaybackSpeedIncrement;
        if (newValue >= Constants.PlaybackSpeedMaximum) newValue = Constants.PlaybackSpeedMaximum;
        PlaybackSpeedSlider.Value = newValue;
    }

    private void HandleSpeedDecrease()
    {
        PlaybackSpeedSliderFlyout.ShowAt(PlaybackSpeedButton);
        var newValue = PlaybackSpeedSlider.Value - Constants.PlaybackSpeedIncrement;
        if (newValue <= Constants.PlaybackSpeedMinimum) newValue = Constants.PlaybackSpeedMinimum;
        PlaybackSpeedSlider.Value = newValue;
    }

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