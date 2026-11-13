// Author: rstewa · https://github.com/rstewa
// Updated: 07/30/2025

using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;
using Audibly.App.Extensions;
using Audibly.App.Helpers;
using Audibly.App.UserControls;
using Audibly.App.ViewModels;
using CommunityToolkit.WinUI;
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

    public NewMiniPlayerPage()
    {
        InitializeComponent();
        KeyDown += NewMiniPlayerPage_KeyDown;

        ProgressSlider.AddHandler(PointerPressedEvent,
            new PointerEventHandler(Slider_OnPointerPressed), true);
        ProgressSlider.AddHandler(PointerReleasedEvent,
            new PointerEventHandler(Slider_OnPointerReleased), true);
        ProgressSlider.AddHandler(PointerCanceledEvent,
            new PointerEventHandler(Slider_OnPointerReleased), true);
        ProgressSlider.Loaded += ProgressSlider_Loaded;
    }

    private void ProgressSlider_Loaded(object sender, RoutedEventArgs e)
    {
        ProgressSlider.ApplyTemplate();
        var trackLayout = ProgressSlider.FindDescendant<Grid>(grid => grid.Name == "HorizontalTemplate");
        if (trackLayout == null || trackLayout.RowDefinitions.Count < 3) return;

        // Template row definitions can retain the theme's default spacing instead of
        // the slider's local resources. Apply these directly to align the bar with its labels.
        trackLayout.MinHeight = ProgressSlider.Height;
        trackLayout.RowDefinitions[0].Height = new GridLength((double)ProgressSlider.Resources["SliderPreContentMargin"]);
        trackLayout.RowDefinitions[1].Height = new GridLength((double)ProgressSlider.Resources["SliderTrackThemeHeight"]);
        trackLayout.RowDefinitions[2].Height = new GridLength((double)ProgressSlider.Resources["SliderPostContentMargin"]);
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
            SpeedControl.Nudge(-1);
            args.Handled = true;
        }
        else if (key == (VirtualKey)221) // Close bracket ']'
        {
            SpeedControl.Nudge(1);
            args.Handled = true;
        }
        else if (key == (VirtualKey)0xDC) // Backslash '\'
        {
            SpeedControl.ResetToNormal();
            args.Handled = true;
        }
    }

    private bool _isPointerInteracting;
    private double _pendingSeekValue;

    private void Slider_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isPointerInteracting = true;
        PlayerViewModel.IsUserSeeking = true;
    }

    private async void Slider_OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_isPointerInteracting) return;
        _isPointerInteracting = false;

        await PlayerViewModel.SeekToPositionAsync(_pendingSeekValue);
    }

    private async void Slider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        _pendingSeekValue = e.NewValue;

        // If not dragging (e.g. a tap/click to seek), commit immediately
        if (!_isPointerInteracting && PlayerViewModel.IsUserSeeking)
        {
            await PlayerViewModel.SeekToPositionAsync(e.NewValue);
        }
    }

    private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        var slider = sender as Slider;
        if (slider == null || !IsLoaded) return;

        PlayerViewModel.UpdateVolume(slider.Value);
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

    private async void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        ExitButton.IsEnabled = false;
        PlayerViewModel.AudioPlayer.Pause();
        try
        {
            if (PlayerViewModel.NowPlaying != null)
                await PlayerViewModel.NowPlaying.SaveAsync();
        }
        catch (Exception exception)
        {
            ViewModel.LoggingService.LogError(exception, false);
        }

        PlayerViewModel.Dispose();
        Application.Current.Exit();
    }

    private void TimerMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && int.TryParse(item.Tag?.ToString(), out var seconds))
            PlayerViewModel.SetTimer(seconds);
    }

    private void CustomTimerMenuItem_Click(object sender, RoutedEventArgs e)
    {
        // Create the popup
        var timerPopup = new CustomTimerPopup();

        // Find the timer button and position the popup above it

        // get timer button's position using its x:name
        var timerButton = TimerButton;
        if (timerButton == null)
        {
            // Fallback if the button is not found
            timerPopup.Show(XamlRoot);
            return;
        }

        // Show the popup above the button
        timerPopup.ShowAbove(timerButton, XamlRoot);
    }

    private void CancelTimerMenuItem_Click(object sender, RoutedEventArgs e)
    {
        PlayerViewModel.SetTimer(0);
    }

    private void EndOfChapterTimerMenuItem_Click(object sender, RoutedEventArgs e)
    {
        PlayerViewModel.SetEndOfChapterTimer();
    }
}
