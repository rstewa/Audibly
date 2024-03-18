// Author: rstewa · https://github.com/rstewa
// Created: 3/11/2024
// Updated: 3/17/2024

using System;
using System.Linq;
using Windows.Media.Playback;
using Audibly.App.Extensions;
using Audibly.App.ViewModels;
using Audibly.Models;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
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
        await _dispatcherQueue.EnqueueAsync(async () =>
        {
            ChapterCombo.SelectedIndex = PlayerViewModel.NowPlaying.CurrentChapterIndex ?? 0;
            PlayerViewModel.ChapterDurationMs = (int)(PlayerViewModel.NowPlaying.CurrentChapter.EndTime -
                                                      PlayerViewModel.NowPlaying.CurrentChapter.StartTime);
            
            // todo: set current position here from audiobook record
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
        _dispatcherQueue.TryEnqueue(() =>
        {
            // NowPlayingBar.Value = sender.Position.TotalSeconds;
            // ChapterPositionText.Text = sender.Position.ToString(@"hh\:mm\:ss");
            if (!PlayerViewModel.NowPlaying.CurrentChapter.InRange(_currentPosition.TotalMilliseconds))
            {
                var tmp = PlayerViewModel.NowPlaying.Chapters.FirstOrDefault(c => c.InRange(_currentPosition.TotalMilliseconds));
                if (tmp != null)
                {
                    PlayerViewModel.NowPlaying.CurrentChapter = tmp;
                    PlayerViewModel.ChapterDurationMs = (int)(PlayerViewModel.NowPlaying.CurrentChapter.EndTime -
                                                              PlayerViewModel.NowPlaying.CurrentChapter.StartTime);
                }
            }

            PlayerViewModel.ChapterPositionMs = _currentPosition.TotalMilliseconds > PlayerViewModel.NowPlaying.CurrentChapter.StartTime
                ? (int)(_currentPosition.TotalMilliseconds - PlayerViewModel.NowPlaying.CurrentChapter.StartTime)
                : 0;

            PlayerViewModel.ChapterPositionText = PlayerViewModel.ChapterPositionMs.ToStr_ms();

            ChapterCombo.SelectedIndex = ChapterCombo.Items.IndexOf(PlayerViewModel.NowPlaying.CurrentChapter);
        });
    }

    private void NowPlayingBar_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        // if (_isDragging)
        // {
        //     _currentPosition = TimeSpan.FromMilliseconds(e.NewValue);
        // }
    }

    private void NowPlayingBar_OnDragStarting(UIElement sender, DragStartingEventArgs args)
    {
        _isDragging = true;
    }

    private void NowPlayingBar_OnDropCompleted(UIElement sender, DropCompletedEventArgs args)
    {
        _isDragging = false;
        _currentPosition = TimeSpan.FromMilliseconds(NowPlayingBar.Value);
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
        var chapter = (ChapterInfo)ChapterCombo.SelectedItem;
        if (chapter == null)
        {
            ChapterCombo.SelectedIndex = PlayerViewModel.NowPlaying.CurrentChapterIndex ?? 0;
        }
        else
        {
            // TODO: i feel like this is gross ...
            PlayerViewModel.NowPlaying.CurrentChapterIndex = PlayerViewModel.NowPlaying.Chapters.IndexOf(chapter);
            PlayerViewModel.NowPlaying.CurrentChapter = chapter;
            // AudioPlayer.MediaPlayer.PlaybackSession.Position = TimeSpan.FromMilliseconds(chapter.StartTimeMs);
        }

        // throw new NotImplementedException();
        // todo
    }

    private void PreviousChapterButton_Click(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void NextChapterButton_Click(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void NowPlayingBar_OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        var slider = sender as Slider;
        if (slider != null)
        {
            _currentPosition = TimeSpan.FromMilliseconds(slider.Value);
        }
    }

    private void SkipBackButton_OnClick(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void SkipForwardButton_OnClick(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
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