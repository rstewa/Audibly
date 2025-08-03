// Author: rstewa · https://github.com/rstewa
// Updated: 08/02/2025

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Windows.Media.Core;
using Windows.Media.Playback;
using Audibly.App.Extensions;
using Audibly.App.Helpers;
using Audibly.App.Services;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.ViewModels;

public class PlayerViewModel : BindableBase, IDisposable
{
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    /// <summary>
    ///     Gets the app-wide MediaPlayer instance.
    /// </summary>
    public readonly MediaPlayer MediaPlayer = new();

    private int _chapterComboSelectedIndex;

    private long _chapterDurationMs;

    private string _chapterDurationText = "0:00:00";

    private long _chapterPositionMs;

    private string _chapterPositionText = "0:00:00";

    private bool _isPlayerFullScreen;
    private bool _isTimerActive;

    private string _maximizeMinimizeGlyph = Constants.MaximizeGlyph;

    private string _maximizeMinimizeTooltip = Constants.MaximizeTooltip;

    private AudiobookViewModel? _nowPlaying;

    private bool _pendingAutoPlay;

    private double _playbackSpeed = 1.0;

    private Symbol _playPauseIcon = Symbol.Play;

    private Timer? _sleepTimer;
    private DateTime _timerEndTime;

    private double _timerValue;

    private double _volumeLevel;

    private string _volumeLevelGlyph = Constants.VolumeGlyph3;

    public PlayerViewModel()
    {
        InitializeAudioPlayer();
    }

    /// <summary>
    ///     Gets or sets the currently playing audiobook.
    /// </summary>
    public AudiobookViewModel? NowPlaying
    {
        get => _nowPlaying;
        set => Set(ref _nowPlaying, value);
    }

    /// <summary>
    ///     Gets or sets the timer value for the player.
    /// </summary>
    public double TimerValue
    {
        get => _timerValue;
        set => Set(ref _timerValue, value);
    }

    /// <summary>
    ///     Gets or sets the glyph for the volume level.
    /// </summary>
    public string VolumeLevelGlyph
    {
        get => _volumeLevelGlyph;
        set => Set(ref _volumeLevelGlyph, value);
    }

    /// <summary>
    ///     Gets or sets the volume level.
    /// </summary>
    public double VolumeLevel
    {
        get => _volumeLevel;
        set => Set(ref _volumeLevel, value);
    }

    /// <summary>
    ///     Gets or sets the playback speed.
    /// </summary>
    public double PlaybackSpeed
    {
        get => _playbackSpeed;
        set => Set(ref _playbackSpeed, value);
    }

    /// <summary>
    ///     Gets or sets the chapter duration text.
    /// </summary>
    public string ChapterDurationText
    {
        get => _chapterDurationText;
        set => Set(ref _chapterDurationText, value);
    }

    /// <summary>
    ///     Gets or sets the chapter position text.
    /// </summary>
    public string ChapterPositionText
    {
        get => _chapterPositionText;
        set => Set(ref _chapterPositionText, value);
    }

    /// <summary>
    ///     Gets or sets the chapter position in milliseconds.
    /// </summary>
    public int ChapterPositionMs
    {
        get => (int)_chapterPositionMs;
        set
        {
            Set(ref _chapterPositionMs, value);
            ChapterPositionText = _chapterPositionMs.ToStr_ms();
        }
    }

    /// <summary>
    ///     Gets or sets the chapter duration in milliseconds.
    /// </summary>
    public int ChapterDurationMs
    {
        get => (int)_chapterDurationMs;
        set
        {
            Set(ref _chapterDurationMs, value);
            ChapterDurationText = _chapterDurationMs.ToStr_ms();
        }
    }

    /// <summary>
    ///     Gets or sets a value indicating whether the player is in full screen mode.
    /// </summary>
    public bool IsPlayerFullScreen
    {
        get => _isPlayerFullScreen;
        set => Set(ref _isPlayerFullScreen, value);
    }

    /// <summary>
    ///     Gets or sets the glyph for the maximize/minimize button.
    /// </summary>
    public string MaximizeMinimizeGlyph
    {
        get => _maximizeMinimizeGlyph;
        set => Set(ref _maximizeMinimizeGlyph, value);
    }

    /// <summary>
    ///     Gets or sets the tooltip for the maximize/minimize button.
    /// </summary>
    public string MaximizeMinimizeTooltip
    {
        get => _maximizeMinimizeTooltip;
        set => Set(ref _maximizeMinimizeTooltip, value);
    }

    /// <summary>
    ///     Gets or sets the play/pause icon.
    /// </summary>
    public Symbol PlayPauseIcon
    {
        get => _playPauseIcon;
        set => Set(ref _playPauseIcon, value);
    }

    /// <summary>
    ///     Gets or sets the selected index of the chapter combo box.
    /// </summary>
    public int ChapterComboSelectedIndex
    {
        get => _chapterComboSelectedIndex;
        set => Set(ref _chapterComboSelectedIndex, value);
    }

    /// <summary>
    ///     Gets or sets the current position of the media player.
    /// </summary>
    public TimeSpan CurrentPosition
    {
        get => MediaPlayer.PlaybackSession.Position;
        set => MediaPlayer.PlaybackSession.Position = value > TimeSpan.Zero ? value : TimeSpan.Zero;
    }

    /// <summary>
    /// </summary>
    public bool IsTimerActive
    {
        get => _isTimerActive;
        private set => Set(ref _isTimerActive, value);
    }

    /// <summary>
    /// </summary>
    public string TimerRemainingText
    {
        get => _isTimerActive ? FormatTimeRemaining() : "Timer Off";
        private set => OnPropertyChanged();
    }

    #region methods

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

        // set volume level from settings
        // UpdateVolume(UserSettings.Volume);
        // UpdatePlaybackSpeed(UserSettings.PlaybackSpeed);
    }

    public void SetTimer(double seconds)
    {
        // Cancel existing timer if active
        if (_sleepTimer != null)
        {
            _sleepTimer.Stop();
            _sleepTimer.Dispose();
            _sleepTimer = null;
        }

        // Disable timer if seconds is 0
        if (seconds <= 0)
        {
            TimerValue = 0;
            IsTimerActive = false;
            OnPropertyChanged(nameof(TimerRemainingText));
            return;
        }

        // Create and start new timer
        _timerEndTime = DateTime.Now.AddSeconds(seconds);
        TimerValue = seconds;
        IsTimerActive = true;

        _sleepTimer = new Timer(1000); // Update every second
        _sleepTimer.Elapsed += SleepTimer_Elapsed;
        _sleepTimer.Start();

        OnPropertyChanged(nameof(TimerRemainingText));
    }

    private void SleepTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        var timeRemaining = _timerEndTime - DateTime.Now;

        if (timeRemaining <= TimeSpan.Zero)
            // Timer expired - pause playback
            _dispatcherQueue.TryEnqueue(() =>
            {
                MediaPlayer.Pause();
                IsTimerActive = false;
                _sleepTimer?.Stop();
                _sleepTimer?.Dispose();
                _sleepTimer = null;
                OnPropertyChanged(nameof(TimerRemainingText));
            });
        else
            // Update remaining time display
            _dispatcherQueue.TryEnqueue(() => { OnPropertyChanged(nameof(TimerRemainingText)); });
    }

    private string FormatTimeRemaining()
    {
        var timeRemaining = _timerEndTime - DateTime.Now;
        if (timeRemaining <= TimeSpan.Zero)
            return "Timer Off";

        return timeRemaining.TotalHours >= 1
            ? $"{(int)timeRemaining.TotalHours}:{timeRemaining:mm\\:ss}"
            : $"{timeRemaining:mm\\:ss}";
    }

    public void Dispose()
    {
        _sleepTimer?.Stop();
        _sleepTimer?.Dispose();
    }

    public async void UpdateVolume(double volume)
    {
        VolumeLevel = volume;
        MediaPlayer.Volume = volume / 100;
        VolumeLevelGlyph = volume switch
        {
            > 66 => Constants.VolumeGlyph3,
            > 33 => Constants.VolumeGlyph2,
            > 0 => Constants.VolumeGlyph1,
            _ => Constants.VolumeGlyph0
        };

        // save volume level for audiobook
        if (NowPlaying == null) return;

        NowPlaying.Volume = volume;
        NowPlaying.IsModified = true;
        await NowPlaying.SaveAsync();
    }

    public async void UpdatePlaybackSpeed(double speed)
    {
        PlaybackSpeed = speed;
        MediaPlayer.PlaybackRate = speed;

        // save playback speed for audiobook
        if (NowPlaying == null) return;

        NowPlaying.PlaybackSpeed = speed;
        NowPlaying.IsModified = true;
        await NowPlaying.SaveAsync();
    }

    public async Task OpenAudiobook(AudiobookViewModel audiobook)
    {
        if (NowPlaying != null && NowPlaying.Equals(audiobook))
            return;

        // todo: trying this out
        if (NowPlaying != null)
        {
            NowPlaying.IsNowPlaying = false;

            await NowPlaying.SaveAsync();
        }

        MediaPlayer.Pause();

        App.ViewModel.SelectedAudiobook = audiobook;

        // verify that the file exists
        // if there are multiple source files, check them all

        if (audiobook.SourcePaths.Any(sourceFile => !File.Exists(sourceFile.FilePath)))
        {
            // note: content dialog
            await DialogService.ShowErrorDialogAsync("Error",
                $"Unable to play audiobook: {audiobook.Title}. One or more of its source files were deleted or moved.");

            return;
        }

        await _dispatcherQueue.EnqueueAsync(async () =>
        {
            NowPlaying = audiobook;

            if (NowPlaying.DateLastPlayed == null)
            {
                // use the global playback speed and volume level if they are set
                // and this is the first time the audiobook is being played
                UpdatePlaybackSpeed(UserSettings.PlaybackSpeed);
                UpdateVolume(UserSettings.Volume);
            }
            else
            {
                // use the audiobook's playback speed and volume level
                UpdatePlaybackSpeed(NowPlaying.PlaybackSpeed);
                UpdateVolume(NowPlaying.Volume);
            }

            NowPlaying.IsNowPlaying = true;
            NowPlaying.DateLastPlayed = DateTime.Now;

            ChapterComboSelectedIndex = NowPlaying.CurrentChapterIndex ?? 0;
            NowPlaying.CurrentChapterTitle = NowPlaying.Chapters[ChapterComboSelectedIndex].Title;

            await NowPlaying.SaveAsync();
        });

        MediaPlayer.Source = MediaSource.CreateFromUri(audiobook.CurrentSourceFile.FilePath.AsUri());
    }

    public async void OpenSourceFile(int index, int chapterIndex)
    {
        if (NowPlaying == null || NowPlaying.CurrentSourceFileIndex == index)
            return;

        NowPlaying.CurrentTimeMs = 0;
        NowPlaying.CurrentSourceFileIndex = index;
        NowPlaying.CurrentChapterIndex = chapterIndex;

        await _dispatcherQueue.EnqueueAsync(() =>
        {
            NowPlaying.CurrentChapterTitle = NowPlaying.Chapters[chapterIndex].Title;
        });

        await NowPlaying.SaveAsync();

        MediaPlayer.Source = MediaSource.CreateFromUri(NowPlaying.CurrentSourceFile.FilePath.AsUri());
    }

    # endregion

    #region event handlers

    private void AudioPlayer_MediaOpened(MediaPlayer sender, object args)
    {
        if (NowPlaying == null) return;
        _dispatcherQueue.EnqueueAsync(async () =>
        {
            if (NowPlaying.Chapters.Count == 0)
            {
                NowPlaying = null;

                // note: content dialog
                await DialogService.ShowErrorDialogAsync("Error",
                    "An error occurred while trying to open the selected audiobook. " +
                    "The chapters could not be loaded. Please try importing the audiobook again.");

                return;
            }

            ChapterComboSelectedIndex = NowPlaying.CurrentChapterIndex ?? 0;

            ChapterDurationMs = (int)(NowPlaying.CurrentChapter.EndTime - NowPlaying.CurrentChapter.StartTime);

            ChapterPositionMs =
                NowPlaying.CurrentTimeMs > NowPlaying.CurrentChapter.StartTime
                    ? (int)(NowPlaying.CurrentTimeMs - NowPlaying.CurrentChapter.StartTime)
                    : 0;

            CurrentPosition = TimeSpan.FromMilliseconds(NowPlaying.CurrentTimeMs);

            if (_pendingAutoPlay)
            {
                _pendingAutoPlay = false;
                MediaPlayer.Play();
            }
        });
    }

    private void AudioPlayer_MediaEnded(MediaPlayer sender, object args)
    {
        // check if there is a next source file
        if (NowPlaying == null || NowPlaying.CurrentSourceFileIndex >= NowPlaying.SourcePaths.Count - 1) return;

        // todo: log error here
        if (NowPlaying.CurrentChapterIndex == null) return;

        _pendingAutoPlay = true;

        OpenSourceFile(NowPlaying.CurrentSourceFileIndex + 1, (int)NowPlaying.CurrentChapterIndex + 1);
    }

    private void AudioPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        _dispatcherQueue.TryEnqueue(() => NowPlaying = null);

        // note: content dialog
        App.ViewModel.EnqueueNotification(new Notification
        {
            Message =
                "Unable to open the audiobook: media failed. Please verify that the file is not corrupted and try again.",
            Severity = InfoBarSeverity.Error
        });
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
        if (NowPlaying == null) return;

        if (!NowPlaying.CurrentChapter.InRange(CurrentPosition.TotalMilliseconds))
        {
            var newChapter = NowPlaying.Chapters.FirstOrDefault(c =>
                c.ParentSourceFileIndex == NowPlaying.CurrentSourceFileIndex &&
                c.InRange(CurrentPosition.TotalMilliseconds));

            if (newChapter != null)
                _ = _dispatcherQueue.EnqueueAsync(() =>
                {
                    NowPlaying.CurrentChapterIndex = ChapterComboSelectedIndex = newChapter.Index;
                    NowPlaying.CurrentChapterTitle = newChapter.Title;
                    ChapterDurationMs = (int)(NowPlaying.CurrentChapter.EndTime - NowPlaying.CurrentChapter.StartTime);
                });
        }

        _ = _dispatcherQueue.EnqueueAsync(async () =>
        {
            ChapterPositionMs = (int)(CurrentPosition.TotalMilliseconds > NowPlaying.CurrentChapter.StartTime
                ? CurrentPosition.TotalMilliseconds - NowPlaying.CurrentChapter.StartTime
                : 0);
            // ChapterPositionMs = (int)(CurrentPosition.TotalMilliseconds - NowPlaying.CurrentChapter.StartTime);
            NowPlaying.CurrentTimeMs = (int)CurrentPosition.TotalMilliseconds;

            // TODO: this is gross
            // calculate/update progress
            double tmp = 0;
            if (NowPlaying.CurrentSourceFileIndex != 0)
                for (var i = 0; i < NowPlaying.CurrentSourceFileIndex; i++)
                    tmp += NowPlaying.SourcePaths[i].Duration;
            tmp += CurrentPosition.TotalSeconds;
            NowPlaying.Progress = Math.Ceiling(tmp / NowPlaying.Duration * 100);
            NowPlaying.IsCompleted = NowPlaying.Progress >= 99.9;
        });

        await NowPlaying.SaveAsync();
    }

    #endregion
}