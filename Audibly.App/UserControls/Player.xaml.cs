// Author: rstewa · https://github.com/rstewa
// Created: 3/11/2024
// Updated: 3/21/2024

using System;
using System.Linq;
using Windows.Media.Playback;
using Audibly.App.Extensions;
using Audibly.App.Helpers;
using Audibly.App.ViewModels;
using Audibly.App.Views;
using Audibly.Models;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;

namespace Audibly.App.UserControls;

public sealed partial class Player : UserControl
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

    private TimeSpan _currentPosition
    {
        get => PlayerViewModel.MediaPlayer.PlaybackSession.Position;
        set => PlayerViewModel.MediaPlayer.PlaybackSession.Position =
            value > TimeSpan.Zero ? value : TimeSpan.Zero;
    }

    public Player()
    {
        InitializeComponent();
        AudioPlayer.SetMediaPlayer(PlayerViewModel.MediaPlayer);
        InitializeAudioPlayer();

        // todo: load most recently played audiobook into the player
    }

    private void InitializeAudioPlayer()
    {
        PlayerViewModel.MediaPlayer.AutoPlay = false;
        PlayerViewModel.MediaPlayer.AudioCategory = MediaPlayerAudioCategory.Media;
        PlayerViewModel.MediaPlayer.AudioDeviceType = MediaPlayerAudioDeviceType.Multimedia;
        PlayerViewModel.MediaPlayer.CommandManager.IsEnabled = true; // todo: what is this?
        PlayerViewModel.MediaPlayer.MediaOpened += AudioPlayer_MediaOpened;
        PlayerViewModel.MediaPlayer.MediaEnded += AudioPlayer_MediaEnded;
        PlayerViewModel.MediaPlayer.MediaFailed += AudioPlayer_MediaFailed;
        PlayerViewModel.MediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
        PlayerViewModel.MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
    }

    private bool _isDragging;

    private async void AudioPlayer_MediaOpened(MediaPlayer sender, object args)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            PlayerViewModel.NowPlaying.CurrentChapter =
                PlayerViewModel.NowPlaying.Chapters[PlayerViewModel.NowPlaying.CurrentChapterIndex ?? 0];
            ChapterCombo.SelectedIndex = PlayerViewModel.NowPlaying.CurrentChapterIndex ?? 0;
            PlayerViewModel.ChapterDurationMs = (int)(PlayerViewModel.NowPlaying.CurrentChapter.EndTime -
                                                      PlayerViewModel.NowPlaying.CurrentChapter.StartTime);
            PlayerViewModel.ChapterPositionMs =
                _currentPosition.TotalMilliseconds > PlayerViewModel.NowPlaying.CurrentChapter.StartTime
                    ? (int)(_currentPosition.TotalMilliseconds - PlayerViewModel.NowPlaying.CurrentChapter.StartTime)
                    : 0;
            _currentPosition = TimeSpan.FromMilliseconds(PlayerViewModel.NowPlaying.CurrentTimeMs);
        });

        // todo: add toggle player controls function
        // todo: set volume, playback speed
    }

    private void AudioPlayer_MediaEnded(MediaPlayer sender, object args)
    {
        NowPlayingBar.Value = 0;
    }

    private void AudioPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        throw new NotImplementedException();
    }

    private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
    {
        switch (sender.PlaybackState)
        {
            case MediaPlaybackState.Playing:
                DispatcherQueue.TryEnqueue(() =>
                {
                    if ((string)PlayPauseButton.Tag == "pause") return;
                    PlayPauseButton.Tag = "pause";
                    PlayPauseIcon.Symbol = Symbol.Pause;
                });

                break;

            case MediaPlaybackState.Paused:
                DispatcherQueue.TryEnqueue(() =>
                {
                    if ((string)PlayPauseButton.Tag == "play") return;
                    PlayPauseButton.Tag = "play";
                    PlayPauseIcon.Symbol = Symbol.Play;
                });

                break;
        }
    }

    private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
    {
        _dispatcherQueue.EnqueueAsync(async () =>
        {
            if (!PlayerViewModel.NowPlaying.CurrentChapter.InRange(_currentPosition.TotalMilliseconds))
            {
                var tmp = PlayerViewModel.NowPlaying.Chapters.FirstOrDefault(c =>
                    c.InRange(_currentPosition.TotalMilliseconds));
                if (tmp != null)
                {
                    PlayerViewModel.NowPlaying.CurrentChapter = tmp;
                    PlayerViewModel.NowPlaying.CurrentChapterIndex = PlayerViewModel.NowPlaying.Chapters.IndexOf(tmp);
                    ChapterCombo.SelectedIndex = ChapterCombo.Items.IndexOf(PlayerViewModel.NowPlaying.CurrentChapter);
                    PlayerViewModel.ChapterDurationMs = (int)(PlayerViewModel.NowPlaying.CurrentChapter.EndTime -
                                                              PlayerViewModel.NowPlaying.CurrentChapter.StartTime);
                }
            }


            PlayerViewModel.ChapterPositionMs = (int)(_currentPosition.TotalMilliseconds -
                                                      PlayerViewModel.NowPlaying.CurrentChapter.StartTime);
            PlayerViewModel.ChapterPositionText = PlayerViewModel.ChapterPositionMs.ToStr_ms();
            PlayerViewModel.NowPlaying.CurrentTimeMs = (int)_currentPosition.TotalMilliseconds;

            // todo: find out what the performance impact of this is
            await PlayerViewModel.NowPlaying.SaveAsync();
        });
    }

    private void PlayPauseButton_OnClick(object sender, RoutedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if ((string)PlayPauseButton.Tag == "play")
                PlayerViewModel.MediaPlayer.Play();
            else
                PlayerViewModel.MediaPlayer.Pause();
        });
    }

    private void ChapterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var container = sender as ComboBox;
        if (container == null || container.SelectedItem is not ChapterInfo chapter) return;

        if (ChapterCombo.SelectedIndex == ChapterCombo.Items.IndexOf(PlayerViewModel.NowPlaying.CurrentChapter)) return;

        _currentPosition = TimeSpan.FromMilliseconds(chapter.StartTime);

        // var chapter = (ChapterInfo)ChapterCombo.SelectedItem;
        // if (chapter == null)
        // {
        //     ChapterCombo.SelectedIndex = PlayerViewModel.NowPlaying.CurrentChapterIndex ?? 0;
        // }
        // else
        // {
        //     // TODO: i feel like this is gross ...
        //     PlayerViewModel.NowPlaying.CurrentChapterIndex = PlayerViewModel.NowPlaying.Chapters.IndexOf(chapter);
        //     PlayerViewModel.NowPlaying.CurrentChapter = chapter;
        //     // AudioPlayer.MediaPlayer.PlaybackSession.Position = TimeSpan.FromMilliseconds(chapter.StartTimeMs);
        // }

        // throw new NotImplementedException();
        // todo
    }

    private void PreviousChapterButton_Click(object sender, RoutedEventArgs e)
    {
        var newChapterIndex = PlayerViewModel.NowPlaying.CurrentChapterIndex - 1 >= 0
            ? PlayerViewModel.NowPlaying.CurrentChapterIndex - 1
            : PlayerViewModel.NowPlaying.CurrentChapterIndex;

        if (newChapterIndex == null) return;

        PlayerViewModel.NowPlaying.CurrentChapter = PlayerViewModel.NowPlaying.Chapters[(int)newChapterIndex];
        PlayerViewModel.NowPlaying.CurrentChapterIndex = newChapterIndex;
        ChapterCombo.SelectedIndex = (int)newChapterIndex;
        _currentPosition = TimeSpan.FromMilliseconds(PlayerViewModel.NowPlaying.CurrentChapter.StartTime);
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
        ChapterCombo.SelectedIndex = (int)newChapterIndex;
        _currentPosition = TimeSpan.FromMilliseconds(PlayerViewModel.NowPlaying.CurrentChapter.StartTime);
    }

    private void NowPlayingBar_OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        var slider = sender as Slider;
        // todo: not sure about this 2nd check here
        if (slider != null && slider.Value != 0)
            _currentPosition =
                TimeSpan.FromMilliseconds(PlayerViewModel.NowPlaying.CurrentChapter.StartTime + slider.Value);
    }

    private static readonly TimeSpan _skipBackButtonAmount = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan _skipForwardButtonAmount = TimeSpan.FromSeconds(30);

    private void SkipBackButton_OnClick(object sender, RoutedEventArgs e)
    {
        _currentPosition = _currentPosition - _skipBackButtonAmount > TimeSpan.Zero
            ? _currentPosition - _skipBackButtonAmount
            : TimeSpan.Zero;
    }

    private void SkipForwardButton_OnClick(object sender, RoutedEventArgs e)
    {
        // todo: might need to switch this to using the duration from the audiobook record
        _currentPosition = _currentPosition + _skipForwardButtonAmount <=
                           PlayerViewModel.MediaPlayer.PlaybackSession.NaturalDuration
            ? _currentPosition + _skipForwardButtonAmount
            : PlayerViewModel.MediaPlayer.PlaybackSession.NaturalDuration;
    }

    private void OpenMiniPlayerButton_OnClick(object sender, RoutedEventArgs e)
    {
        var window = WindowHelper.CreateWindow();

        // C# code to create a new window
        // todo: check if there is already an instance of the mini player open and if so, bring it to the front
        var newWindow = WindowHelper.CreateWindow();

        const int width = 315;
        const int height = 420;

        newWindow.CustomizeWindow(width, height, true, true, false, false, false);

        // newWindow.SetTitleBar(DefaultApp);
        var rootPage = new MiniPlayerPage();
        // rootPage.RequestedTheme = ThemeHelper.RootTheme;
        newWindow.Content = rootPage;
        newWindow.Activate();
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