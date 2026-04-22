// Author: rstewa · https://github.com/rstewa
// Updated: 08/02/2025

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Audibly.App.Extensions;
using Audibly.App.Helpers;
using Audibly.App.Services;
using CommunityToolkit.WinUI;
using LibVLCSharp.Shared;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.ViewModels;

public class PlayerViewModel : BindableBase, IDisposable
{
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    private readonly LibVLC _libVLC;
    private readonly LibVLCSharp.Shared.MediaPlayer _mediaPlayer;

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

    private bool _mediaJustOpened;

    private bool _skipSeeking;

    public PlayerViewModel()
    {
        _libVLC = new LibVLC("--no-video", "--audio-resampler=samplerate", "--src-converter-type=1");
        _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);
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
        get => TimeSpan.FromMilliseconds(_mediaPlayer.Time >= 0 ? _mediaPlayer.Time : 0);
        set
        {
            if (!_skipSeeking)
            {
                var currentPositionMs = Math.Max(0, (long)value.TotalMilliseconds);
                _mediaPlayer.Time = currentPositionMs;

                if (NowPlaying == null) return;

                if (!NowPlaying.CurrentChapter.InRange(currentPositionMs))
                {
                    var newChapter = NowPlaying.Chapters.FirstOrDefault(c =>
                        c.ParentSourceFileIndex == NowPlaying.CurrentSourceFileIndex &&
                        c.InRange(currentPositionMs));

                    if (newChapter != null)
                    {
                        _skipSeeking = true;
                        NowPlaying.CurrentChapterIndex = ChapterComboSelectedIndex = newChapter.Index;
                        NowPlaying.CurrentChapterTitle = newChapter.Title;
                        ChapterDurationMs = (int)(NowPlaying.CurrentChapter.EndTime - NowPlaying.CurrentChapter.StartTime);
                        _skipSeeking = false;
                    }
                }

                ChapterPositionMs = (int)(currentPositionMs > NowPlaying.CurrentChapter.StartTime
                    ? currentPositionMs - NowPlaying.CurrentChapter.StartTime
                    : 0);
                NowPlaying.CurrentTimeMs = (int)currentPositionMs;

                double tmp = 0;
                if (NowPlaying.CurrentSourceFileIndex != 0)
                    for (var i = 0; i < NowPlaying.CurrentSourceFileIndex; i++)
                        tmp += NowPlaying.SourcePaths[i].Duration;
                tmp += currentPositionMs / 1000.0;
                NowPlaying.Progress = Math.Ceiling(tmp / NowPlaying.Duration * 100);
                NowPlaying.IsCompleted = NowPlaying.Progress >= 99.9;
            }
        }
    }

    /// <summary>
    ///     Gets the natural duration of the currently loaded media.
    /// </summary>
    public TimeSpan NaturalDuration =>
        TimeSpan.FromMilliseconds(_mediaPlayer.Length >= 0 ? _mediaPlayer.Length : 0);

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

    /// <summary>
    ///     Starts or resumes playback.
    /// </summary>
    public void Play()
    {
        if (_mediaPlayer.State == VLCState.Ended)
        {
            // If media has ended, stop to get the player back into a state where it can play
            _mediaPlayer.Stop();
        }
        _mediaPlayer.Play();
    }

    /// <summary>
    ///     Pauses playback.
    /// </summary>
    public void Pause()
    {
        _mediaPlayer.SetPause(true);
    }

    #region methods

    private void InitializeAudioPlayer()
    {
        //Enable hardware acceleartion.
        _mediaPlayer.EnableHardwareDecoding = true;

        // LibVLC events fire on background threads — dispatch to UI thread.
        _mediaPlayer.Playing += OnPlaying;
        _mediaPlayer.Paused += OnPaused;
        _mediaPlayer.EndReached += OnEndReached;
        _mediaPlayer.EncounteredError += OnEncounteredError;
        _mediaPlayer.TimeChanged += OnTimeChanged;
    }

    private void OnPlaying(object? sender, EventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (PlayPauseIcon == Symbol.Pause) return;
            PlayPauseIcon = Symbol.Pause;

            // Handle "media opened" logic on first Playing event after setting new media.
            if (_mediaJustOpened)
                HandleMediaOpened();
        });
    }

    private void OnPaused(object? sender, EventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (PlayPauseIcon == Symbol.Play) return;
            PlayPauseIcon = Symbol.Play;
        });
    }

    private void OnEndReached(object? sender, EventArgs e)
    {
        // CRITICAL: Must not call Play/SetMedia from EndReached handler — LibVLC deadlocks.
        // Dispatch to UI thread to defer the operation.
        _dispatcherQueue.TryEnqueue(() => HandleMediaEnded());
    }

    private void OnEncounteredError(object? sender, EventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            _mediaJustOpened = false;
            NowPlaying = null;
        });

        App.ViewModel.EnqueueNotification(new Notification
        {
            Message =
                "Unable to open the audiobook: media failed. Please verify that the file is not corrupted and try again.",
            Severity = InfoBarSeverity.Error
        });
    }

    private void OnTimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (NowPlaying == null || _mediaJustOpened || IsUserSeeking ) return;

            var currentPositionMs = e.Time;

            if (!NowPlaying.CurrentChapter.InRange(currentPositionMs))
            {
                var newChapter = NowPlaying.Chapters.FirstOrDefault(c =>
                    c.ParentSourceFileIndex == NowPlaying.CurrentSourceFileIndex &&
                    c.InRange(currentPositionMs));

                if (newChapter != null)
                {
                    _skipSeeking = true;
                    NowPlaying.CurrentChapterIndex = ChapterComboSelectedIndex = newChapter.Index;
                    NowPlaying.CurrentChapterTitle = newChapter.Title;
                    ChapterDurationMs = (int)(NowPlaying.CurrentChapter.EndTime - NowPlaying.CurrentChapter.StartTime);
                    _skipSeeking = false;
                }
                ;
            }


            ChapterPositionMs = (int)(currentPositionMs > NowPlaying.CurrentChapter.StartTime
                ? currentPositionMs - NowPlaying.CurrentChapter.StartTime
                : 0);
            NowPlaying.CurrentTimeMs = (int)currentPositionMs;

            // TODO: this is gross
            // calculate/update progress
            double tmp = 0;
            if (NowPlaying.CurrentSourceFileIndex != 0)
                for (var i = 0; i < NowPlaying.CurrentSourceFileIndex; i++)
                    tmp += NowPlaying.SourcePaths[i].Duration;
            tmp += currentPositionMs / 1000.0;
            NowPlaying.Progress = Math.Ceiling(tmp / NowPlaying.Duration * 100);
            NowPlaying.IsCompleted = NowPlaying.Progress >= 99.9;
            ;

            _ = Task.Run(async () => await NowPlaying.SaveAsync());
        });
    }

    private void HandleMediaOpened()
    {
        if (NowPlaying == null) return;

        if (NowPlaying.Chapters.Count == 0)
        {
            _mediaJustOpened = false;
            NowPlaying = null;

            _ = DialogService.ShowErrorDialogAsync("Error",
                "An error occurred while trying to open the selected audiobook. " +
                "The chapters could not be loaded. Please try importing the audiobook again.");

            return;
        }

        ChapterComboSelectedIndex = NowPlaying.CurrentChapterIndex ?? 0;
        NowPlaying.CurrentChapterTitle = NowPlaying.Chapters[ChapterComboSelectedIndex].Title;
        ChapterDurationMs = (int)(NowPlaying.CurrentChapter.EndTime - NowPlaying.CurrentChapter.StartTime);
        ChapterPositionMs =
            NowPlaying.CurrentTimeMs > NowPlaying.CurrentChapter.StartTime
                ? (int)(NowPlaying.CurrentTimeMs - NowPlaying.CurrentChapter.StartTime)
                : 0;

        // Seek to saved position.
        _mediaPlayer.Time = NowPlaying.CurrentTimeMs;

        _mediaPlayer.SetRate((float)PlaybackSpeed);

        if (_pendingAutoPlay)
        {
            _pendingAutoPlay = false;
            // Already playing since we used _mediaPlayer.Play() to open
        }
        else
        {
            // We started playing to trigger media load — pause now since user didn't press play
            _mediaPlayer.SetPause(true);
        }

        _mediaJustOpened = false;
    }

    private void HandleMediaEnded()
    {
        // check if there is a next source file
        if (NowPlaying == null || NowPlaying.CurrentSourceFileIndex >= NowPlaying.SourcePaths.Count - 1)
        {
            //We have no more media to play make sure to set the play button to play instead of pause!
            if (PlayPauseIcon != Symbol.Play)
                PlayPauseIcon = Symbol.Play;
            return;
        }
        var nextSourceFileIndex = NowPlaying.CurrentSourceFileIndex + 1;
        var nextChapter = NowPlaying.Chapters.FirstOrDefault(c =>
            c.ParentSourceFileIndex == nextSourceFileIndex);

        if (nextChapter == null) return;

        _pendingAutoPlay = true;

        _ = OpenSourceFile(nextSourceFileIndex, nextChapter.Index);
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
                _mediaPlayer.SetPause(true);
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

        _mediaPlayer.Stop();
        _mediaPlayer.Dispose();
        _libVLC.Dispose();
    }

    public async void UpdateVolume(double volume)
    {
        VolumeLevel = volume;
        _mediaPlayer.Volume = (int)volume;
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
        _mediaPlayer.SetRate((float)speed);

        // save playback speed for audiobook
        if (NowPlaying == null) return;

        NowPlaying.PlaybackSpeed = speed;
        NowPlaying.IsModified = true;
        await NowPlaying.SaveAsync();
    }

    public async Task OpenAudiobook(AudiobookViewModel audiobook)
    {
        if (_mediaJustOpened)
            return;

        if (NowPlaying != null && NowPlaying.Equals(audiobook))
            return;

        var playRequested = false;

        try
        {
            if (NowPlaying != null)
            {
                NowPlaying.IsNowPlaying = false;

                await NowPlaying.SaveAsync();
            }

            _mediaPlayer.SetPause(true);

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

            await _dispatcherQueue.EnqueueAsync(() =>
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
                ChapterDurationMs = (int)(NowPlaying.CurrentChapter.EndTime - NowPlaying.CurrentChapter.StartTime);
                ChapterPositionMs =
                    NowPlaying.CurrentTimeMs > NowPlaying.CurrentChapter.StartTime
                        ? (int)(NowPlaying.CurrentTimeMs - NowPlaying.CurrentChapter.StartTime)
                        : 0;
            });

            await NowPlaying.SaveAsync();

            using (var media = new Media(_libVLC, audiobook.CurrentSourceFile.FilePath, FromType.FromPath))
            {
                //this makes vlc ignore chapters usually it's chapter aware but that breaks our existing logic assuming the player position is absolute from file start instead of chapter start.
                media.AddOption(":demux=avformat");
                _mediaJustOpened = true;
                playRequested = _mediaPlayer.Play(media);
            }
        }
        finally
        {
            if (!playRequested)
                _mediaJustOpened = false;
        }
    }

    public async Task OpenSourceFile(int index, int chapterIndex, long currentTimeMs = 0)
    {
        if (_mediaJustOpened)
            return;

        if (NowPlaying == null || NowPlaying.CurrentSourceFileIndex == index)
            return;

        var playRequested = false;

        try
        {
            NowPlaying.CurrentTimeMs = (int)currentTimeMs;
            NowPlaying.CurrentSourceFileIndex = index;
            NowPlaying.CurrentChapterIndex = chapterIndex;
            NowPlaying.CurrentChapterTitle = NowPlaying.Chapters[chapterIndex].Title;
            ChapterComboSelectedIndex = chapterIndex;
            ChapterDurationMs = (int)(NowPlaying.CurrentChapter.EndTime - NowPlaying.CurrentChapter.StartTime);
            ChapterPositionMs =
                currentTimeMs > NowPlaying.CurrentChapter.StartTime
                    ? (int)(currentTimeMs - NowPlaying.CurrentChapter.StartTime)
                    : 0;

            await NowPlaying.SaveAsync();

            using (var media = new Media(_libVLC, NowPlaying.CurrentSourceFile.FilePath, FromType.FromPath))
            {
                media.AddOption(":demux=avformat");
                _mediaJustOpened = true;
                playRequested = _mediaPlayer.Play(media);
            }
        }
        finally
        {
            if (!playRequested)
                _mediaJustOpened = false;
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

    // Event handlers are now private methods called from LibVLC event subscriptions above.

    #endregion
}
