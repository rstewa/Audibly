// Author: rstewa · https://github.com/rstewa
// Updated: 02/18/2025

using System;
using Audibly.App.Extensions;
using Audibly.App.Helpers;
using Audibly.App.ViewModels;
using Audibly.App.Views;
using Audibly.Models;
using Microsoft.UI.Composition.SystemBackdrops;
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
        // check if there is already an instance of the mini player open and if so, bring it to the front
        var existingWindow = WindowHelper.GetMiniPlayerWindow();
        if (existingWindow != null)
        {
            existingWindow.Activate();
            WindowHelper.HideMainWindow();
            return;
        }

        var newWindow = WindowHelper.CreateWindow("MiniPlayerWindow");

        // const int width = 504;

        // check if we're on win 10 or 11
        newWindow.CustomizeWindow(536, !MicaController.IsSupported() ? 92 : 96, true, true, false, false, false);

        var rootPage = new NewMiniPlayerPage();
        newWindow.Content = rootPage;

        newWindow.SetWindowDraggable(true);
        newWindow.RemoveWindowBorderAndTitleBar();
        newWindow.Activate();

        WindowHelper.HideMainWindow();
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
}