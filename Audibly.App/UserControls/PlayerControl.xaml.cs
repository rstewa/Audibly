// Author: rstewa · https://github.com/rstewa
// Created: 4/5/2024
// Updated: 4/6/2024

using System;
using System.Diagnostics;
using System.Linq;
using Audibly.App.Extensions;
using Audibly.App.Helpers;
using Audibly.App.ViewModels;
using Audibly.App.Views;
using Audibly.Models;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Constants = Audibly.App.Helpers.Constants;

namespace Audibly.App.UserControls;

public sealed partial class PlayerControl : UserControl
{
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

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

    public static readonly DependencyProperty ShowCoverImageProperty =
        DependencyProperty.Register(nameof(ShowCoverImage), typeof(bool), typeof(PlayerControl),
            new PropertyMetadata(true));

    public PlayerControl()
    {
        InitializeComponent();
        AudioPlayer.SetMediaPlayer(PlayerViewModel.MediaPlayer);

        // todo: load most recently played audiobook into the player
    }

    private void PlayPauseButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (PlayerViewModel.PlayPauseIcon == Symbol.Play)
            PlayerViewModel.MediaPlayer.Play();
        else
            PlayerViewModel.MediaPlayer.Pause();
    }

    private void ChapterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var container = sender as ComboBox;
        if (container == null || container.SelectedItem is not ChapterInfo chapter) return;

        if (ChapterCombo.SelectedIndex == ChapterCombo.Items.IndexOf(PlayerViewModel.NowPlaying.CurrentChapter)) return;

        PlayerViewModel.CurrentPosition = TimeSpan.FromMilliseconds(chapter.StartTime);
    }

    private void PreviousChapterButton_Click(object sender, RoutedEventArgs e)
    {
        var newChapterIndex = PlayerViewModel.NowPlaying.CurrentChapterIndex - 1 >= 0
            ? PlayerViewModel.NowPlaying.CurrentChapterIndex - 1
            : PlayerViewModel.NowPlaying.CurrentChapterIndex;

        if (newChapterIndex == null) return;

        PlayerViewModel.NowPlaying.CurrentChapter = PlayerViewModel.NowPlaying.Chapters[(int)newChapterIndex];
        PlayerViewModel.NowPlaying.CurrentChapterIndex = newChapterIndex;
        PlayerViewModel.ChapterComboSelectedIndex = (int)newChapterIndex;
        PlayerViewModel.CurrentPosition =
            TimeSpan.FromMilliseconds(PlayerViewModel.NowPlaying.CurrentChapter.StartTime);
    }

    private void NextChapterButton_Click(object sender, RoutedEventArgs e)
    {
        var newChapterIndex =
            PlayerViewModel.NowPlaying.CurrentChapterIndex + 1 < PlayerViewModel.NowPlaying.Chapters.Count
                ? PlayerViewModel.NowPlaying.CurrentChapterIndex + 1
                : PlayerViewModel.NowPlaying.CurrentChapterIndex;

        if (newChapterIndex == null) return;

        PlayerViewModel.NowPlaying.CurrentChapter = PlayerViewModel.NowPlaying.Chapters[(int)newChapterIndex];
        PlayerViewModel.NowPlaying.CurrentChapterIndex = newChapterIndex;
        PlayerViewModel.ChapterComboSelectedIndex = (int)newChapterIndex;
        PlayerViewModel.CurrentPosition =
            TimeSpan.FromMilliseconds(PlayerViewModel.NowPlaying.CurrentChapter.StartTime);
    }

    private void NowPlayingBar_OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        var slider = sender as Slider;
        if (slider != null && slider.Value != 0)
            PlayerViewModel.CurrentPosition =
                TimeSpan.FromMilliseconds(PlayerViewModel.NowPlaying.CurrentChapter.StartTime + slider.Value);
    }

    private static readonly TimeSpan _skipBackButtonAmount = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan _skipForwardButtonAmount = TimeSpan.FromSeconds(30);

    private void SkipBackButton_OnClick(object sender, RoutedEventArgs e)
    {
        PlayerViewModel.CurrentPosition = PlayerViewModel.CurrentPosition - _skipBackButtonAmount > TimeSpan.Zero
            ? PlayerViewModel.CurrentPosition - _skipBackButtonAmount
            : TimeSpan.Zero;
    }

    private void SkipForwardButton_OnClick(object sender, RoutedEventArgs e)
    {
        // todo: might need to switch this to using the duration from the audiobook record
        PlayerViewModel.CurrentPosition = PlayerViewModel.CurrentPosition + _skipForwardButtonAmount <=
                                          PlayerViewModel.MediaPlayer.PlaybackSession.NaturalDuration
            ? PlayerViewModel.CurrentPosition + _skipForwardButtonAmount
            : PlayerViewModel.MediaPlayer.PlaybackSession.NaturalDuration;
    }

    private void OpenMiniPlayerButton_OnClick(object sender, RoutedEventArgs e)
    {
        // check if there is already an instance of the mini player open and if so, bring it to the front
        foreach (var window in WindowHelper.ActiveWindows)
        {
            if (window.Content is not LegacyPlayerPage) continue;
            window.Activate();
            return;
        }

        var newWindow = WindowHelper.CreateWindow();

        const int width = 315;
        const int height = 420;

        newWindow.CustomizeWindow(width, height, true, true, false, false, false);

        // newWindow.SetTitleBar(DefaultApp);
        var rootPage = new LegacyPlayerPage();
        // rootPage.RequestedTheme = ThemeHelper.RootTheme;
        newWindow.Content = rootPage;
        newWindow.Activate();
    }

    // TODO
    private void Player_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (PlayerViewModel.NowPlaying == null && ViewModel.Audiobooks.Any(a => a.IsNowPlaying))
            PlayerViewModel.NowPlaying = ViewModel.Audiobooks.FirstOrDefault(a => a.IsNowPlaying);
    }

    private void MaximizePlayerButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!PlayerViewModel.IsPlayerFullScreen)
        {
            PlayerViewModel.IsPlayerFullScreen = true;
            PlayerViewModel.MaximizeMinimizeGlyph = Constants.MinimizeGlyph;

            var playerWindow = WindowHelper.CreateWindow();

            // win32WindowHelper = new Win32WindowHelper(playerWindow);
            // win32WindowHelper.SetWindowMinMaxSize(new Win32WindowHelper.POINT { x = 1050, y = 800 });

            playerWindow.Closed += (o, args) =>
            {
                PlayerViewModel.IsPlayerFullScreen = false;
                WindowHelper.RestoreMainWindow();
            };

#if DEBUG
            playerWindow.SizeChanged += (o, args) =>
            {
                Debug.WriteLine($"Player -> Width: {args.Size.Width}, Height: {args.Size.Height}");
            };
#endif

            playerWindow.CustomizeWindow(-1, -1, true, true, true, true, true, true);
            playerWindow.Maximize();
            // todo
            // playerWindow.MakeWindowFullScreen();

            var rootPage = new PlayerPage();
            playerWindow.Content = rootPage;
            playerWindow.Activate();

            WindowHelper.HideMainWindow();
        }
        else
        {
            PlayerViewModel.IsPlayerFullScreen = false;
            PlayerViewModel.MaximizeMinimizeGlyph = Constants.MaximizeGlyph;
            WindowHelper.ActiveWindows.FirstOrDefault(w => w.Content is PlayerPage)?.Close();
            WindowHelper.RestoreMainWindow();
        }
    }

    private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        ;
    }
}

public class ProgressSliderValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var chapterPositionMs = (double)value;
        return chapterPositionMs.ToStr_ms();
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}