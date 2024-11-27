// Author: rstewa · https://github.com/rstewa
// Created: 04/15/2024
// Updated: 10/11/2024

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Core;
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

    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    /// <summary>
    ///     Gets the app-wide MediaPlayer instance.
    /// </summary>
    public readonly MediaPlayer MediaPlayer = new();

    private AudiobookViewModel? _nowPlaying;

    /// <summary>
    ///     Gets or sets the currently playing audiobook.
    /// </summary>
    public AudiobookViewModel? NowPlaying
    {
        get => _nowPlaying;
        set => Set(ref _nowPlaying, value);
    }

    private string _volumeLevelGlyph = Constants.VolumeGlyph3;

    /// <summary> 
    ///     Gets or sets the glyph for the volume level.
    /// </summary>
    public string VolumeLevelGlyph
    {
        get => _volumeLevelGlyph;
        set => Set(ref _volumeLevelGlyph, value);
    }

    private double _volumeLevel;

    /// <summary>
    ///     Gets or sets the volume level.
    /// </summary>
    public double VolumeLevel
    {
        get => _volumeLevel;
        set => Set(ref _volumeLevel, value);
    }

    private double _playbackSpeed = 1.0;

    /// <summary>
    ///     Gets or sets the playback speed.
    /// </summary>
    public double PlaybackSpeed
    {
        get => _playbackSpeed;
        set => Set(ref _playbackSpeed, value);
    }

    private string _chapterDurationText = "0:00:00";

    /// <summary>
    ///     Gets or sets the chapter duration text.
    /// </summary>
    public string ChapterDurationText
    {
        get => _chapterDurationText;
        set => Set(ref _chapterDurationText, value);
    }

    private string _chapterPositionText = "0:00:00";

    /// <summary>
    ///     Gets or sets the chapter position text.
    /// </summary>
    public string ChapterPositionText
    {
        get => _chapterPositionText;
        set => Set(ref _chapterPositionText, value);
    }

    private long _chapterPositionMs;

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

    private long _chapterDurationMs;

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

    private bool _isPlayerFullScreen;

    /// <summary>
    ///     Gets or sets a value indicating whether the player is in full screen mode.
    /// </summary>
    public bool IsPlayerFullScreen
    {
        get => _isPlayerFullScreen;
        set => Set(ref _isPlayerFullScreen, value);
    }

    private string _maximizeMinimizeGlyph = Constants.MaximizeGlyph;

    /// <summary>
    ///     Gets or sets the glyph for the maximize/minimize button.
    /// </summary>
    public string MaximizeMinimizeGlyph
    {
        get => _maximizeMinimizeGlyph;
        set => Set(ref _maximizeMinimizeGlyph, value);
    }

    private string _maximizeMinimizeTooltip = Constants.MaximizeTooltip;

    /// <summary>
    ///     Gets or sets the tooltip for the maximize/minimize button.
    /// </summary>
    public string MaximizeMinimizeTooltip
    {
        get => _maximizeMinimizeTooltip;
        set => Set(ref _maximizeMinimizeTooltip, value);
    }

    private Symbol _playPauseIcon = Symbol.Play;

    /// <summary>
    ///     Gets or sets the play/pause icon.
    /// </summary>
    public Symbol PlayPauseIcon
    {
        get => _playPauseIcon;
        set => Set(ref _playPauseIcon, value);
    }

    private int _chapterComboSelectedIndex;

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
        UpdateVolume(UserSettings.Volume);
        UpdatePlaybackSpeed(UserSettings.PlaybackSpeed);
    }

    public void UpdateVolume(double volume)
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

        // save volume level to settings
        UserSettings.Volume = volume;
    }

    public void UpdatePlaybackSpeed(double speed)
    {
        PlaybackSpeed = speed;
        MediaPlayer.PlaybackRate = speed;

        // save playback speed to settings
        UserSettings.PlaybackSpeed = speed;
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

        // todo: not sure setting selected audiobook is necessary
        App.ViewModel.SelectedAudiobook = audiobook;

        // verify that the file exists
        // if there are multiple source files, check them all
        
        if (audiobook.SourcePaths.Any(sourceFile => !File.Exists(sourceFile.FilePath)))
        {
            App.ViewModel.MessageService.ShowDialog(DialogType.Error, "Error",
                $"Can't play Audiobook: {audiobook.Title}. One of its source files was deleted or moved.");
            return;
        }

        NowPlaying = audiobook;
        NowPlaying.IsNowPlaying = true;
        NowPlaying.DateLastPlayed = DateTime.Now;

        await NowPlaying.SaveAsync();

        MediaPlayer.Source = MediaSource.CreateFromUri(audiobook.CurrentSourceFile.FilePath.AsUri());
    }

    public async void OpenSourceFile(int index, int chapterIndex)
    {
        if (NowPlaying == null || NowPlaying.CurrentSourceFileIndex == index)
            return;

        NowPlaying.CurrentTimeMs = 0;
        NowPlaying.CurrentSourceFileIndex = index;
        NowPlaying.CurrentChapterIndex = chapterIndex;

        await NowPlaying.SaveAsync();

        MediaPlayer.Source = MediaSource.CreateFromUri(NowPlaying.CurrentSourceFile.FilePath.AsUri());
    }
    
    # endregion
    
    #region event handlers

    private void AudioPlayer_MediaOpened(MediaPlayer sender, object args)
    {
        if (NowPlaying == null) return;
        _ = _dispatcherQueue.TryEnqueue(() =>
        {
            if (NowPlaying.Chapters.Count == 0)
            {
                NowPlaying = null;

                // todo: ask if they want to re-import the audiobook
                App.ViewModel.MessageService.ShowDialog(DialogType.Error, "Error",
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
        });
    }

    private void AudioPlayer_MediaEnded(MediaPlayer sender, object args)
    {
        // check if there is a next source file
        if (NowPlaying == null || NowPlaying.CurrentSourceFileIndex >= NowPlaying.SourcePaths.Count - 1) return;

        // todo: log error here
        if (NowPlaying.CurrentChapterIndex == null) return;

        OpenSourceFile(NowPlaying.CurrentSourceFileIndex + 1, (int)NowPlaying.CurrentChapterIndex + 1);
        MediaPlayer.Play();
    }

    private void AudioPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        _dispatcherQueue.TryEnqueue(() => NowPlaying = null);

        App.ViewModel.MessageService.ShowDialog(DialogType.Error, "Error",
            "An error occurred while trying to play the selected audiobook. " +
            "Please verify that the file is not corrupted and try again.");
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