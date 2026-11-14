// Author: rstewa · https://github.com/rstewa
// Updated: 08/02/2025

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Windows.Media;
using Windows.Media.Playback;
using Audibly.App.Extensions;
using Audibly.App.Helpers;
using Audibly.App.Services;
using Audibly.App.Services.Audio;
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
    public MediaPlayer MediaPlayer => AudioPlayer.MediaPlayer;

    public PitchPreservingPlayer AudioPlayer { get; }

    private int _chapterComboSelectedIndex;

    private long _chapterDurationMs;

    private string _chapterDurationText = "0:00:00";

    private long _chapterPositionMs;

    private string _chapterPositionText = "0:00:00";

    private bool _isPlayerFullScreen;
    private bool _isTimerActive;
    private bool _disposed;

    private string _maximizeMinimizeGlyph = Constants.MaximizeGlyph;

    private string _maximizeMinimizeTooltip = Constants.MaximizeTooltip;

    private AudiobookViewModel? _nowPlaying;

    private bool _pendingAutoPlay;

    private double _playbackSpeed = 1.0;

    private Symbol _playPauseIcon = Symbol.Play;

    private Timer? _sleepTimer;
    private ChapterSleepTimer? _chapterSleepTimer;
    private object? _sleepTimerStoppedSource;
    private DateTime _timerEndTime;

    private double _timerValue;

    private double _volumeLevel;

    private string _volumeLevelGlyph = Constants.VolumeGlyph3;

    public PlayerViewModel()
    {
        AudioPlayer = new PitchPreservingPlayer(action => _dispatcherQueue.TryEnqueue(() => action()));
        InitializeAudioPlayer();
    }

    /// <summary>
    ///     Gets or sets whether the user is currently seeking (dragging the slider).
    ///     When true, the PositionChanged handler will not update ChapterPositionMs
    ///     so that the slider doesn't fight with the user's drag.
    /// </summary>
    public bool IsUserSeeking { get; set; }

    /// <summary>
    ///     Gets or sets the currently playing audiobook.
    /// </summary>
    public AudiobookViewModel? NowPlaying
    {
        get => _nowPlaying;
        set
        {
            if (ReferenceEquals(_nowPlaying, value)) return;
            if (_chapterSleepTimer != null) SetTimer(0);
            if (_nowPlaying != null) _nowPlaying.PropertyChanged -= NowPlaying_PropertyChanged;
            Set(ref _nowPlaying, value);
            if (_nowPlaying != null) _nowPlaying.PropertyChanged += NowPlaying_PropertyChanged;
            NotifyRemainingTimesChanged();
        }
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
        set
        {
            if (Set(ref _playbackSpeed, value))
            {
                NotifyRemainingTimesChanged();
                if (_chapterSleepTimer != null) OnPropertyChanged(nameof(TimerRemainingText));
            }
        }
    }

    public string ChapterRemainingText => NowPlaying == null
        ? "—"
        : PlaybackTimeRemaining.Format(ChapterDurationMs, ChapterPositionMs, PlaybackSpeed);

    public string BookRemainingText
    {
        get
        {
            var book = NowPlaying;
            if (book == null || book.CurrentSourceFileIndex < 0 || book.CurrentSourceFileIndex >= book.SourcePaths.Count)
                return "—";

            return PlaybackTimeRemaining.ForBook(book.Duration, book.SourcePaths.Select(file => (double)file.Duration),
                book.CurrentSourceFileIndex, book.CurrentTimeMs, PlaybackSpeed);
        }
    }

    private void NotifyRemainingTimesChanged()
    {
        OnPropertyChanged(nameof(ChapterRemainingText));
        OnPropertyChanged(nameof(BookRemainingText));
    }

    private void NowPlaying_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) ||
            e.PropertyName is nameof(AudiobookViewModel.CurrentTimeMs) or
                nameof(AudiobookViewModel.CurrentSourceFileIndex) or nameof(AudiobookViewModel.Model))
            OnPropertyChanged(nameof(BookRemainingText));
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
            OnPropertyChanged(nameof(ChapterRemainingText));
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
            OnPropertyChanged(nameof(ChapterRemainingText));
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
        get => AudioPlayer.Position;
        set => AudioPlayer.Position = value;
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
        MediaPlayer.CommandManager.RateReceived += CommandManager_RateReceived;
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
        _chapterSleepTimer = null;
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

        StartSleepTimer(seconds, 1000);
    }

    public void SetEndOfChapterTimer()
    {
        var book = NowPlaying;
        if (book == null || book.Chapters.Count == 0) return;

        var timer = new ChapterSleepTimer(book.SourcePaths.Select(file => (double)file.Duration).ToArray(),
            book.CurrentChapter.ParentSourceFileIndex, book.CurrentChapter.EndTime);
        var remaining = timer.GetRemainingTime(book.CurrentSourceFileIndex, CurrentPosition.TotalMilliseconds, PlaybackSpeed);
        SetTimer(0);
        if (remaining <= TimeSpan.Zero) return;

        _chapterSleepTimer = timer;
        StartSleepTimer(remaining.TotalSeconds, 100);
    }

    private void StartSleepTimer(double seconds, double intervalMs)
    {
        _timerEndTime = DateTime.Now.AddSeconds(seconds);
        TimerValue = seconds;
        IsTimerActive = true;

        _sleepTimer = new Timer(intervalMs);
        _sleepTimer.Elapsed += SleepTimer_Elapsed;
        _sleepTimer.Start();

        OnPropertyChanged(nameof(TimerRemainingText));
    }

    private void SleepTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            // Ignore ticks already queued by a cancelled or replaced timer.
            if (_disposed || !IsTimerActive || !ReferenceEquals(sender, _sleepTimer)) return;
            if (_chapterSleepTimer != null)
            {
                if (TryCompleteChapterSleepTimer()) return;
            }
            else if (DateTime.Now >= _timerEndTime)
            {
                StopForSleepTimer();
                return;
            }

            OnPropertyChanged(nameof(TimerRemainingText));
        });
    }

    private bool TryCompleteChapterSleepTimer(bool sourceEnded = false)
    {
        if (!IsTimerActive || _chapterSleepTimer == null || NowPlaying == null || AudioPlayer.IsOpening)
            return false;

        var positionMs = sourceEnded ? double.PositiveInfinity : CurrentPosition.TotalMilliseconds;
        if (!_chapterSleepTimer.HasReachedEnd(NowPlaying.CurrentSourceFileIndex, positionMs)) return false;

        StopForSleepTimer();
        return true;
    }

    private void StopForSleepTimer()
    {
        // A queued MediaEnded event must not start the next file after the timer has paused playback.
        _sleepTimerStoppedSource = MediaPlayer.Source;
        AudioPlayer.Pause();
        SetTimer(0);
    }

    private string FormatTimeRemaining()
    {
        var timeRemaining = _chapterSleepTimer != null && NowPlaying != null
            ? _chapterSleepTimer.GetRemainingTime(NowPlaying.CurrentSourceFileIndex, CurrentPosition.TotalMilliseconds, PlaybackSpeed)
            : _timerEndTime - DateTime.Now;
        if (timeRemaining <= TimeSpan.Zero)
            return "Timer Off";

        if (_chapterSleepTimer != null)
            timeRemaining = TimeSpan.FromSeconds(Math.Ceiling(timeRemaining.TotalSeconds));

        return timeRemaining.TotalHours >= 1
            ? $"{(int)timeRemaining.TotalHours}:{timeRemaining:mm\\:ss}"
            : $"{timeRemaining:mm\\:ss}";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (NowPlaying != null) NowPlaying.PropertyChanged -= NowPlaying_PropertyChanged;
        _sleepTimer?.Stop();
        _sleepTimer?.Dispose();
        MediaPlayer.MediaOpened -= AudioPlayer_MediaOpened;
        MediaPlayer.MediaEnded -= AudioPlayer_MediaEnded;
        MediaPlayer.MediaFailed -= AudioPlayer_MediaFailed;
        MediaPlayer.CommandManager.RateReceived -= CommandManager_RateReceived;
        MediaPlayer.PlaybackSession.PositionChanged -= PlaybackSession_PositionChanged;
        MediaPlayer.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
        AudioPlayer.Dispose();
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
        speed = PlaybackSpeedPresets.Snap(speed);
        try
        {
            AudioPlayer.Speed = speed;
        }
        catch (Exception)
        {
            App.ViewModel.EnqueueNotification(new Notification
            {
                Message = "Unable to change playback speed. Please try reopening the audiobook.",
                Severity = InfoBarSeverity.Error
            });
            return;
        }

        PlaybackSpeed = speed;

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

        AudioPlayer.Pause();

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

        AudioPlayer.Close();
        _pendingAutoPlay = false;

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

        OpenAudioSource(TimeSpan.FromMilliseconds(audiobook.CurrentTimeMs), false);
    }

    public async Task OpenSourceFile(int index, int chapterIndex)
    {
        if (NowPlaying == null || NowPlaying.CurrentSourceFileIndex == index)
            return;

        var autoPlay = _pendingAutoPlay || MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing;
        _pendingAutoPlay = false;
        AudioPlayer.Close();
        NowPlaying.CurrentTimeMs = (int)NowPlaying.Chapters[chapterIndex].StartTime;
        NowPlaying.CurrentSourceFileIndex = index;
        NowPlaying.CurrentChapterIndex = chapterIndex;

        await _dispatcherQueue.EnqueueAsync(() =>
        {
            NowPlaying.CurrentChapterTitle = NowPlaying.Chapters[chapterIndex].Title;
        });

        await NowPlaying.SaveAsync();

        OpenAudioSource(TimeSpan.FromMilliseconds(NowPlaying.CurrentTimeMs), autoPlay);
    }

    private void OpenAudioSource(TimeSpan position, bool autoPlay)
    {
        if (NowPlaying == null) return;
        _sleepTimerStoppedSource = null;
        try
        {
            AudioPlayer.Open(NowPlaying.CurrentSourceFile.FilePath, position, autoPlay);
        }
        catch (Exception)
        {
            ReportMediaFailure();
        }
    }

    /// <summary>
    ///     Seeks to the specified slider value within the current chapter.
    /// </summary>
    public async Task SeekToPositionAsync(double sliderValue)
    {
        if (NowPlaying?.CurrentChapter == null) return;

        IsUserSeeking = false;
        CurrentPosition =
            TimeSpan.FromMilliseconds(NowPlaying.CurrentChapter.StartTime + sliderValue);
        await NowPlaying.SaveAsync();
    }

    #endregion

    #region event handlers

    private void CommandManager_RateReceived(MediaPlaybackCommandManager sender,
        MediaPlaybackCommandManagerRateReceivedEventArgs args)
    {
        args.Handled = true;
        var speed = args.PlaybackRate;
        _dispatcherQueue.TryEnqueue(() => { if (!_disposed) UpdatePlaybackSpeed(speed); });
    }

    private void AudioPlayer_MediaOpened(MediaPlayer sender, object args)
    {
        if (NowPlaying == null) return;
        var openedSource = sender.Source;
        _dispatcherQueue.EnqueueAsync(async () =>
        {
            if (_disposed || NowPlaying == null || !ReferenceEquals(openedSource, MediaPlayer.Source)) return;
            if (NowPlaying.Chapters.Count == 0)
            {
                AudioPlayer.Close();
                NowPlaying = null;

                // note: content dialog
                await DialogService.ShowErrorDialogAsync("Error",
                    "An error occurred while trying to open the selected audiobook. " +
                    "The chapters could not be loaded. Please try importing the audiobook again.");

                return;
            }

            ChapterComboSelectedIndex = NowPlaying.CurrentChapterIndex ?? 0;

            ChapterDurationMs = (int)(NowPlaying.CurrentChapter.EndTime - NowPlaying.CurrentChapter.StartTime);

            ChapterPositionMs = (int)Math.Max(0, CurrentPosition.TotalMilliseconds - NowPlaying.CurrentChapter.StartTime);

            var display = MediaPlayer.SystemMediaTransportControls.DisplayUpdater;
            display.Type = MediaPlaybackType.Music;
            display.MusicProperties.Title = NowPlaying.Title ?? string.Empty;
            display.MusicProperties.Artist = NowPlaying.Author ?? string.Empty;
            display.Update();
        });
    }

    private void AudioPlayer_MediaEnded(MediaPlayer sender, object args)
    {
        var endedSource = sender.Source;
        _dispatcherQueue.TryEnqueue(async () =>
        {
            if (_disposed || !ReferenceEquals(endedSource, MediaPlayer.Source) || AudioPlayer.IsOpening) return;
            PlayPauseIcon = Symbol.Play;
            if (ReferenceEquals(endedSource, _sleepTimerStoppedSource) || TryCompleteChapterSleepTimer(sourceEnded: true))
                return;
            // check if there is a next source file
            if (NowPlaying == null || NowPlaying.CurrentSourceFileIndex >= NowPlaying.SourcePaths.Count - 1) return;

            var nextSourceFileIndex = NowPlaying.CurrentSourceFileIndex + 1;

            // find the first chapter belonging to the next source file
            var nextChapter = NowPlaying.Chapters.FirstOrDefault(c =>
                c.ParentSourceFileIndex == nextSourceFileIndex);

            if (nextChapter == null) return;

            _pendingAutoPlay = true;

            await OpenSourceFile(nextSourceFileIndex, nextChapter.Index);
        });
    }

    private void AudioPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        var failedSource = sender.Source;
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (!_disposed && ReferenceEquals(failedSource, MediaPlayer.Source)) ReportMediaFailure();
        });
    }

    private void ReportMediaFailure()
    {
        AudioPlayer.Close();
        NowPlaying = null;
        PlayPauseIcon = Symbol.Play;

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
                    if (sender.PlaybackState != MediaPlaybackState.Playing) return;
                    _sleepTimerStoppedSource = null;
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

    private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
    {
        var source = MediaPlayer.Source;
        _dispatcherQueue.TryEnqueue(async () =>
        {
            if (_disposed || NowPlaying == null || AudioPlayer.IsOpening || source == null ||
                !ReferenceEquals(source, MediaPlayer.Source)) return;

            var audiobook = NowPlaying;
            TryCompleteChapterSleepTimer();
            var position = CurrentPosition;
            if (!audiobook.CurrentChapter.InRange(position.TotalMilliseconds))
            {
                var newChapter = audiobook.Chapters.FirstOrDefault(c =>
                    c.ParentSourceFileIndex == audiobook.CurrentSourceFileIndex &&
                    c.InRange(position.TotalMilliseconds));

                if (newChapter != null)
                {
                    audiobook.CurrentChapterIndex = ChapterComboSelectedIndex = newChapter.Index;
                    audiobook.CurrentChapterTitle = newChapter.Title;
                    ChapterDurationMs = (int)(audiobook.CurrentChapter.EndTime - audiobook.CurrentChapter.StartTime);
                }
            }

            if (IsUserSeeking) return;

            ChapterPositionMs = (int)Math.Max(0, position.TotalMilliseconds - audiobook.CurrentChapter.StartTime);
            audiobook.CurrentTimeMs = (int)position.TotalMilliseconds;

            var elapsed = position.TotalSeconds;
            for (var i = 0; i < audiobook.CurrentSourceFileIndex; i++)
                elapsed += audiobook.SourcePaths[i].Duration;
            audiobook.Progress = Math.Ceiling(elapsed / audiobook.Duration * 100);
            audiobook.IsCompleted = audiobook.Progress >= 99.9;
            await audiobook.SaveAsync();
        });
    }

    #endregion
}
