// Author: rstewa · https://github.com/rstewa
// Created: 3/29/2024
// Updated: 4/6/2024

using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Audibly.App.Extensions;
using Audibly.App.Helpers;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.ViewModels;

public class PlayerViewModel : BindableBase
{
    public PlayerViewModel()
    {
        InitializeAudioPlayer();
    }

    /// <summary>
    ///     Gets the app-wide MediaPlayer instance.
    /// </summary>
    public readonly MediaPlayer MediaPlayer = new();

    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    private AudiobookViewModel? _nowPlaying;

    public AudiobookViewModel? NowPlaying
    {
        get => _nowPlaying;
        set => Set(ref _nowPlaying, value);
    }

    private string _volumeLevelGlyph = Constants.VolumeGlyph3;

    public string VolumeLevelGlyph
    {
        get => _volumeLevelGlyph;
        set => Set(ref _volumeLevelGlyph, value);
    }

    private double _volumeLevel = 1.0;

    public double VolumeLevel
    {
        get => _volumeLevel;
        set => Set(ref _volumeLevel, value);
    }

    private string _chapterDurationText = "0:00:00";

    public string ChapterDurationText
    {
        get => _chapterDurationText;
        set => Set(ref _chapterDurationText, value);
    }

    private string _chapterPositionText = "0:00:00";

    public string ChapterPositionText
    {
        get => _chapterPositionText;
        set => Set(ref _chapterPositionText, value);
    }

    public void Update(double curMs)
    {
        if (!NowPlaying.CurrentChapter.InRange(curMs))
        {
            var tmp = NowPlaying.Chapters.FirstOrDefault(c => c.InRange(curMs));
            if (tmp != null) NowPlaying.CurrentChapter = tmp;
        }

        ChapterPositionMs = curMs > NowPlaying.CurrentChapter.StartTime
            ? (int)(curMs - NowPlaying.CurrentChapter.StartTime)
            : 0;
        // ChapterPositionText = ChapterPositionMs.ToStr_ms();
        ChapterPositionText = "11:11:11";
        // CurPosInBook = curMs.ToStr_ms();
    }

    private long _chapterPositionMs;

    public int ChapterPositionMs
    {
        get => (int)_chapterPositionMs;
        set
        {
            Set(ref _chapterPositionMs, value);
            ChapterPositionText = _chapterPositionMs.ToStr_ms();
        }
    }

    private long _chapterDurationMs;

    public int ChapterDurationMs
    {
        get => (int)_chapterDurationMs;
        set
        {
            Set(ref _chapterDurationMs, value);
            ChapterDurationText = _chapterDurationMs.ToStr_ms();
        }
    }

    private bool _isPlayerFullScreen;

    public bool IsPlayerFullScreen
    {
        get => _isPlayerFullScreen;
        set => Set(ref _isPlayerFullScreen, value);
    }

    private string _maximizeMinimizeGlyph = Constants.MaximizeGlyph;

    public string MaximizeMinimizeGlyph
    {
        get => _maximizeMinimizeGlyph;
        set => Set(ref _maximizeMinimizeGlyph, value);
    }

    private Symbol _playPauseIcon = Symbol.Play;

    public Symbol PlayPauseIcon
    {
        get => _playPauseIcon;
        set => Set(ref _playPauseIcon, value);
    }

    private int _chapterComboSelectedIndex;

    public int ChapterComboSelectedIndex
    {
        get => _chapterComboSelectedIndex;
        set => Set(ref _chapterComboSelectedIndex, value);
    }

    public TimeSpan CurrentPosition
    {
        get => MediaPlayer.PlaybackSession.Position;
        set => MediaPlayer.PlaybackSession.Position = value > TimeSpan.Zero ? value : TimeSpan.Zero;
    }

    private void InitializeAudioPlayer()
    {
        MediaPlayer.AutoPlay = false;
        MediaPlayer.AudioCategory = MediaPlayerAudioCategory.Media;
        MediaPlayer.AudioDeviceType = MediaPlayerAudioDeviceType.Multimedia;
        MediaPlayer.CommandManager.IsEnabled = true; // todo: what is this?
        MediaPlayer.MediaOpened += AudioPlayer_MediaOpened;
        MediaPlayer.MediaEnded += AudioPlayer_MediaEnded;
        MediaPlayer.MediaFailed += AudioPlayer_MediaFailed;
        MediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
        MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
    }

    private void AudioPlayer_MediaOpened(MediaPlayer sender, object args)
    {
        _ = _dispatcherQueue.TryEnqueue(() =>
        {
            NowPlaying.CurrentChapter = NowPlaying.Chapters[NowPlaying.CurrentChapterIndex ?? 0];

            ChapterComboSelectedIndex = NowPlaying.CurrentChapterIndex ?? 0;
            // ChapterCombo.SelectedIndex = NowPlaying.CurrentChapterIndex ?? 0;

            ChapterDurationMs = (int)(NowPlaying.CurrentChapter.EndTime - NowPlaying.CurrentChapter.StartTime);

            ChapterPositionMs =
                CurrentPosition.TotalMilliseconds > NowPlaying.CurrentChapter.StartTime
                    ? (int)(CurrentPosition.TotalMilliseconds - NowPlaying.CurrentChapter.StartTime)
                    : 0;

            CurrentPosition = TimeSpan.FromMilliseconds(NowPlaying.CurrentTimeMs);
        });

        // todo: add toggle player controls function
        // todo: set volume, playback speed
    }

    private void AudioPlayer_MediaEnded(MediaPlayer sender, object args)
    {
        ; // todo: implement
    }

    private async void AudioPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        var dialog = new ContentDialog
        {
            Title = "Media Failed",
            Content = "Media playback failed.",
            CloseButtonText = "Ok"
        };

        _ = await dialog.ShowAsync();
    }

    private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
    {
        switch (sender.PlaybackState)
        {
            case MediaPlaybackState.Playing:
                _dispatcherQueue.TryEnqueue(() =>
                {
                    if (PlayPauseIcon == Symbol.Pause) return;
                    PlayPauseIcon = Symbol.Pause;
                });

                break;

            case MediaPlaybackState.Paused:
                _dispatcherQueue.TryEnqueue(() =>
                {
                    if (PlayPauseIcon == Symbol.Play) return;
                    PlayPauseIcon = Symbol.Play;
                });

                break;
        }
    }

    private async void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
    {
        if (!NowPlaying.CurrentChapter.InRange(CurrentPosition.TotalMilliseconds))
        {
            var tmp = NowPlaying.Chapters.FirstOrDefault(c =>
                c.InRange(CurrentPosition.TotalMilliseconds));
            if (tmp != null)
                _ = _dispatcherQueue.EnqueueAsync(() =>
                {
                    NowPlaying.CurrentChapter = tmp;
                    NowPlaying.CurrentChapterIndex = NowPlaying.Chapters.IndexOf(tmp);
                    ChapterComboSelectedIndex = NowPlaying.Chapters.IndexOf(NowPlaying.CurrentChapter);
                    ChapterDurationMs = (int)(NowPlaying.CurrentChapter.EndTime - NowPlaying.CurrentChapter.StartTime);
                    return Task.CompletedTask;
                });
        }

        _ = _dispatcherQueue.EnqueueAsync(() =>
        {
            ChapterPositionMs = (int)(CurrentPosition.TotalMilliseconds -
                                      NowPlaying.CurrentChapter.StartTime);
            ChapterPositionText = ChapterPositionMs.ToStr_ms();
            NowPlaying.CurrentTimeMs = (int)CurrentPosition.TotalMilliseconds;

            if (MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
            {
                var tmp = CurrentPosition.TotalSeconds;
                NowPlaying.Progress = Math.Floor(tmp / NowPlaying.Duration * 100);
            }

            return Task.CompletedTask;

            // todo: find out what the performance impact of this is
        });
        
        await NowPlaying.SaveAsync();
    }
}