// Author: rstewa · https://github.com/rstewa
// Updated: 08/02/2025

using System;
using Audibly.App.Extensions;
using Audibly.App.Helpers;
using Audibly.App.ViewModels;
using Audibly.App.Views;
using Audibly.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Constants = Audibly.App.Helpers.Constants;

namespace Audibly.App.UserControls;

public sealed partial class PlayerControlGrid : UserControl
{
    public static readonly DependencyProperty ShowCoverImageProperty =
        DependencyProperty.Register(nameof(ShowCoverImage), typeof(bool), typeof(PlayerControlGrid),
            new PropertyMetadata(true));

    public PlayerControlGrid()
    {
        InitializeComponent();
        AudioPlayer.SetMediaPlayer(PlayerViewModel.MediaPlayer);
    }

    /// <summary>
    ///     Gets the app-wide ViewModel instance.
    /// </summary>
    public MainViewModel ViewModel => App.ViewModel;

    /// <summary>
    ///     Gets the app-wide PlayerViewModel instance.
    /// </summary>
    public PlayerViewModel PlayerViewModel => App.PlayerViewModel;

    public bool ShowCoverImage
    {
        get => (bool)GetValue(ShowCoverImageProperty);
        set => SetValue(ShowCoverImageProperty, value);
    }

    private async void ChapterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var container = sender as ComboBox;
        if (container == null || container.SelectedItem is not ChapterInfo chapter) return;

        // get newly selected chapter
        var newChapter = container.SelectedItem as ChapterInfo;

        // check if the newly selected chapter is the same as the current chapter
        if (PlayerViewModel.NowPlaying?.CurrentChapter != null &&
            PlayerViewModel.NowPlaying.CurrentChapter.Equals(newChapter)) return;

        if (newChapter == null) return;

        // check if the newly selected chapter is in a different source file than the current chapter
        if (PlayerViewModel.NowPlaying != null &&
            PlayerViewModel.NowPlaying.CurrentSourceFile.Index != newChapter.ParentSourceFileIndex)
        {
            // set the current source file index to the new source file index
            PlayerViewModel.OpenSourceFile(newChapter.ParentSourceFileIndex, newChapter.Index);
            PlayerViewModel.CurrentPosition = TimeSpan.FromMilliseconds(newChapter.StartTime);
        }
        else if (ChapterCombo.SelectedIndex != ChapterCombo.Items.IndexOf(PlayerViewModel.NowPlaying?.CurrentChapter))
        {
            PlayerViewModel.CurrentPosition = TimeSpan.FromMilliseconds(newChapter.StartTime);
        }

        await PlayerViewModel.NowPlaying.SaveAsync();
    }

    private void OpenMiniPlayerButton_OnClick(object sender, RoutedEventArgs e)
    {
        WindowHelper.ShowMiniPlayer();
    }

    private void MaximizePlayerButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!PlayerViewModel.IsPlayerFullScreen)
        {
            PlayerViewModel.IsPlayerFullScreen = true;
            PlayerViewModel.MaximizeMinimizeGlyph = Constants.MinimizeGlyph;
            PlayerViewModel.MaximizeMinimizeTooltip = Constants.MinimizeTooltip;

            if (App.RootFrame?.Content is not PlayerPage)
                App.RootFrame?.Navigate(typeof(PlayerPage));

            // App.Window.MakeWindowFullScreen();
        }
        else
        {
            PlayerViewModel.IsPlayerFullScreen = false;
            PlayerViewModel.MaximizeMinimizeGlyph = Constants.MaximizeGlyph;
            PlayerViewModel.MaximizeMinimizeTooltip = Constants.MaximizeTooltip;
            if (App.RootFrame?.Content is PlayerPage)
                App.RootFrame?.Navigate(typeof(AppShell));
            // App.Window.RestoreWindow();
        }
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

    private void TimerButton_OnClick(object sender, RoutedEventArgs e)
    {
        // todo
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
        if (PlayerViewModel.NowPlaying?.CurrentChapter == null) return;

        var endTime = PlayerViewModel.NowPlaying.CurrentChapter.EndTime;
        var currentPosition = PlayerViewModel.CurrentPosition.TotalMilliseconds;
        var timerDuration = endTime - currentPosition;

        // convert to seconds
        timerDuration = timerDuration.ToSeconds().ToInt();

        if (timerDuration > 0) PlayerViewModel.SetTimer(timerDuration);
    }
}