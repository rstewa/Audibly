using System;
using Audibly.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.UserControls;

public sealed partial class PlaySkipButtonsStack : UserControl
{
    private static readonly TimeSpan _skipBackButtonAmount = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan _skipForwardButtonAmount = TimeSpan.FromSeconds(30);

    public static readonly DependencyProperty SpacingProperty = DependencyProperty.Register(
        nameof(Spacing), typeof(double), typeof(PlaySkipButtonsStack), new PropertyMetadata(0.0));

    public PlaySkipButtonsStack()
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

    // spacing dependency property
    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    private void PlayPauseButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (PlayerViewModel.PlayPauseIcon == Symbol.Play)
            PlayerViewModel.MediaPlayer.Play();
        else
            PlayerViewModel.MediaPlayer.Pause();
    }

    private async void PreviousChapterButton_Click(object sender, RoutedEventArgs e)
    {
        if (PlayerViewModel.NowPlaying is not null &&
            PlayerViewModel.NowPlaying?.CurrentChapterIndex - 1 > 0 &&
            PlayerViewModel.NowPlaying?.Chapters[(int)(PlayerViewModel.NowPlaying?.CurrentChapterIndex - 1)]
                .ParentSourceFileIndex != PlayerViewModel.NowPlaying?.CurrentSourceFileIndex)
        {
            var newChapterIdx = (int)PlayerViewModel.NowPlaying.CurrentChapterIndex - 1;
            PlayerViewModel.OpenSourceFile(PlayerViewModel.NowPlaying.CurrentSourceFileIndex - 1, newChapterIdx);
            PlayerViewModel.CurrentPosition =
                TimeSpan.FromMilliseconds(PlayerViewModel.NowPlaying.Chapters[newChapterIdx].StartTime);
            await PlayerViewModel.NowPlaying.SaveAsync();

            return;
        }

        var newChapterIndex = PlayerViewModel.NowPlaying.CurrentChapterIndex - 1 >= 0
            ? PlayerViewModel.NowPlaying.CurrentChapterIndex - 1
            : PlayerViewModel.NowPlaying.CurrentChapterIndex;

        if (newChapterIndex == null) return;

        // PlayerViewModel.NowPlaying.CurrentChapter = PlayerViewModel.NowPlaying.Chapters[(int)newChapterIndex];
        PlayerViewModel.NowPlaying.CurrentChapterIndex = newChapterIndex;
        PlayerViewModel.ChapterComboSelectedIndex = (int)newChapterIndex;
        PlayerViewModel.ChapterDurationMs =
            (int)(PlayerViewModel.NowPlaying.CurrentChapter.EndTime -
                  PlayerViewModel.NowPlaying.CurrentChapter.StartTime);
        PlayerViewModel.CurrentPosition =
            TimeSpan.FromMilliseconds(PlayerViewModel.NowPlaying.CurrentChapter.StartTime);

        await PlayerViewModel.NowPlaying.SaveAsync();
    }

    private async void NextChapterButton_Click(object sender, RoutedEventArgs e)
    {
        if (PlayerViewModel.NowPlaying is not null &&
            PlayerViewModel.NowPlaying?.CurrentChapterIndex + 1 < PlayerViewModel.NowPlaying?.Chapters.Count &&
            PlayerViewModel.NowPlaying?.Chapters[(int)(PlayerViewModel.NowPlaying?.CurrentChapterIndex + 1)]
                .ParentSourceFileIndex != PlayerViewModel.NowPlaying?.CurrentSourceFileIndex)
        {
            var newChapterIdx = (int)PlayerViewModel.NowPlaying.CurrentChapterIndex + 1;
            PlayerViewModel.OpenSourceFile(PlayerViewModel.NowPlaying.CurrentSourceFileIndex + 1, newChapterIdx);
            PlayerViewModel.CurrentPosition =
                TimeSpan.FromMilliseconds(PlayerViewModel.NowPlaying.Chapters[newChapterIdx].StartTime);
            await PlayerViewModel.NowPlaying.SaveAsync();

            return;
        }

        var newChapterIndex =
            PlayerViewModel.NowPlaying.CurrentChapterIndex + 1 < PlayerViewModel.NowPlaying.Chapters.Count
                ? PlayerViewModel.NowPlaying.CurrentChapterIndex + 1
                : PlayerViewModel.NowPlaying.CurrentChapterIndex;

        if (newChapterIndex == null) return;

        // PlayerViewModel.NowPlaying.CurrentChapter = PlayerViewModel.NowPlaying.Chapters[(int)newChapterIndex];
        PlayerViewModel.NowPlaying.CurrentChapterIndex = newChapterIndex;
        PlayerViewModel.ChapterComboSelectedIndex = (int)newChapterIndex;
        PlayerViewModel.ChapterDurationMs =
            (int)(PlayerViewModel.NowPlaying.CurrentChapter.EndTime -
                  PlayerViewModel.NowPlaying.CurrentChapter.StartTime);
        PlayerViewModel.CurrentPosition =
            TimeSpan.FromMilliseconds(PlayerViewModel.NowPlaying.CurrentChapter.StartTime);

        await PlayerViewModel.NowPlaying.SaveAsync();
    }

    private async void SkipBackButton_OnClick(object sender, RoutedEventArgs e)
    {
        PlayerViewModel.CurrentPosition = PlayerViewModel.CurrentPosition - _skipBackButtonAmount > TimeSpan.Zero
            ? PlayerViewModel.CurrentPosition - _skipBackButtonAmount
            : TimeSpan.Zero;

        await PlayerViewModel.NowPlaying.SaveAsync();
    }

    private async void SkipForwardButton_OnClick(object sender, RoutedEventArgs e)
    {
        // todo: might need to switch this to using the duration from the audiobook record
        PlayerViewModel.CurrentPosition = PlayerViewModel.CurrentPosition + _skipForwardButtonAmount <=
                                          PlayerViewModel.MediaPlayer.PlaybackSession.NaturalDuration
            ? PlayerViewModel.CurrentPosition + _skipForwardButtonAmount
            : PlayerViewModel.MediaPlayer.PlaybackSession.NaturalDuration;

        await PlayerViewModel.NowPlaying.SaveAsync();
    }
}