//   Author: Ryan Stewart
//   Date: 03/13/2023

using Audibly.Model;
using FlyleafLib.MediaFramework.MediaDemuxer;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.IO;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using Settings = Audibly.Helpers.AudiblySettingsHelper;

namespace Audibly.Controls;

public sealed partial class DefaultPlayerControl : UserControl
{
    private const double SmartRewindDuration = 0.5; // 0.0;
    private const bool SmartRewind = false; // true;

    private bool CanSmartRewind { get; set; }

    private TimeSpan CurPos
    {
        get => MediaPlayer.PlaybackSession.Position;
        set => MediaPlayer.PlaybackSession.Position = value < TimeSpan.Zero ? TimeSpan.Zero : value;
    }

    private DateTime TimePlayerPaused { get; set; }

    private MediaPlayer MediaPlayer => AudiobookViewModel.Audiobook.MediaPlayer;

    public DefaultPlayerControl()
    {
        InitializeComponent();

        // setting MediaPlayer properties
        AudioPlayerElement.SetMediaPlayer(AudiobookViewModel.Audiobook.MediaPlayer);
        MediaPlayer.AutoPlay = false;
        MediaPlayer.AudioCategory = MediaPlayerAudioCategory.Media;
        MediaPlayer.AudioDeviceType = MediaPlayerAudioDeviceType.Multimedia;
        MediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
        MediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
        MediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
        MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;

        // disable buttons until a book is opened
        ToggleAudioControls(false);

        // means an audiobook wasn't open when the application last closed and/or its the 1st time the application has been run
        if (Settings.CurrentAudiobookPath == null) return;

        // gets and/or sets the current audiobooks metadata and viewmodel
        if (!AudiobookViewModel.Audiobook.Init(Settings.CurrentAudiobookPath)) { return; }

        // I'm sure there's a better way to do this ...
        var file = StorageFile.GetFileFromPathAsync(Settings.CurrentAudiobookPath).GetAwaiter().GetResult();

        MediaPlayerElement_Init(file);
    }

    private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
    {
        switch (sender.PlaybackState)
        {
            case MediaPlaybackState.Playing:
                DispatcherQueue.TryEnqueue(() =>
                {
                    // rewind current playback position 30 sec if book was paused for >= 10 minutes
                    if (SmartRewind && CanSmartRewind && DateTime.UtcNow.Subtract(TimePlayerPaused).TotalMinutes >=
                        SmartRewindDuration)
                    {
                        CurPos -= TimeSpan.FromSeconds(30);
                        CanSmartRewind = false;
                    }

                    if ((string)PlayPauseButton.Tag == "pause") return;
                    PlayPauseButton.Tag = "pause";
                    PlayPauseIcon.Symbol = Symbol.Pause;
                });

                break;

            case MediaPlaybackState.Paused:
                // grab current time for smart rewind feature
                if (SmartRewind)
                {
                    TimePlayerPaused = DateTime.UtcNow;
                    Settings.TimePlayerPaused = TimePlayerPaused;
                    CanSmartRewind = true;
                }

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
        DispatcherQueue.TryEnqueue(() =>
        {
            // this is here to handle an edge case where a SmartRewind should happen but
            // the user has changed the players current playback position before hitting
            // play again
            if (MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Paused)
                CanSmartRewind = false;

            AudiobookViewModel.Audiobook.Update(MediaPlayer.PlaybackSession.Position.TotalMilliseconds);
            ChapterCombo.SelectedIndex = ChapterCombo.Items.IndexOf(AudiobookViewModel.Audiobook.CurChapter);

            Settings.CurrentPosition = CurPos.TotalMilliseconds;
        });
    }

    private async void OpenAudiobook_Click(object sender, RoutedEventArgs e)
    {
        // Create a file picker
        var openPicker = new FileOpenPicker();

        // Retrieve the window handle (HWND) of the current WinUI 3 window.
        // var window = WindowHelper.GetWindowForElement(App.MainWindow);
        var hWnd = WindowNative.GetWindowHandle(App.MainWindow);

        // Initialize the file picker with the window handle (HWND).
        InitializeWithWindow.Initialize(openPicker, hWnd);

        // Set options for your file picker
        openPicker.ViewMode = PickerViewMode.Thumbnail;
        openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        openPicker.FileTypeFilter.Add(".m4b");

        // Open the picker for the user to pick a file
        var file = await openPicker.PickSingleFileAsync();
        if (file is null) return;

        Settings.CurrentAudiobookPath = file.Path;

        if (!AudiobookViewModel.Audiobook.Init(file.Path)) { return; }

        MediaPlayerElement_Init(file);
    }

    private void MediaPlayerElement_Init(IStorageFile file)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            // TODO: the following 2 properties should probably be in the viewmodel
            MediaPlayer.Source = MediaSource.CreateFromStorageFile(file);
            ChapterCombo.SelectedIndex = ChapterCombo.Items.IndexOf(AudiobookViewModel.Audiobook.CurChapter);

            ToggleAudioControls(true);
        });
    }

    private void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            Settings.CurrentBookName = Path.GetFileNameWithoutExtension(AudiobookViewModel.Audiobook.FilePath);

            Settings.CurrentPosition ??= 0;
            CurPos = TimeSpan.FromMilliseconds(Settings.CurrentPosition ?? 0);

            Settings.Volume ??= 100;
            AudiobookViewModel.Audiobook.Volume = Settings.Volume ?? 100;

            Settings.PlaybackSpeed ??= 1;
            AudiobookViewModel.Audiobook.PlaybackSpeed = Settings.PlaybackSpeed ?? 1;

            UpdateVolumeIcon();

            if (Settings.TimePlayerPaused == null)
                CanSmartRewind = false;
            else
                TimePlayerPaused = (DateTime)Settings.TimePlayerPaused;
        });
    }

    private void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        // TODO: add info-bar error message
        ToggleAudioControls(false);
    }

    private void ToggleAudioControls(bool isEnabled)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            PlayPauseButton.IsEnabled = isEnabled;
            PreviousChapterButton.IsEnabled = isEnabled;
            SkipBack10Button.IsEnabled = isEnabled;
            SkipForward30Button.IsEnabled = isEnabled;
            NextChapterButton.IsEnabled = isEnabled;
            ChapterCombo.IsEnabled = isEnabled;
            VolumeLevelButton.IsEnabled = isEnabled;
            PlaybackSpeedButton.IsEnabled = isEnabled;
            CompactViewButton.IsEnabled = isEnabled;

            CurrentTimeTextBlock.Opacity =
                ChapterProgressProgressBar.Opacity =
                    CurrentChapterDurationTextBlock.Opacity = isEnabled ? 1.0 : 0.5;
        });
    }

    private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if ((string)PlayPauseButton.Tag == "play")
                MediaPlayer.Play();
            else
                MediaPlayer.Pause();
        });
    }

    private void NextChapterButton_Click(object sender, RoutedEventArgs e)
    {
        CurPos = AudiobookViewModel.Audiobook.GetNextChapter();
        ChapterCombo.SelectedIndex = ChapterCombo.Items.IndexOf(AudiobookViewModel.Audiobook.CurChapter);
    }

    private void PreviousChapterButton_Click(object sender, RoutedEventArgs e)
    {
        CurPos = AudiobookViewModel.Audiobook.GetPrevChapter(CurPos.TotalMilliseconds);
        ChapterCombo.SelectedIndex = ChapterCombo.Items.IndexOf(AudiobookViewModel.Audiobook.CurChapter);
    }

    private void SkipForward30Button_Click(object sender, RoutedEventArgs e)
    {
        CurPos += TimeSpan.FromSeconds(30);
    }

    private void SkipBack10Button_Click(object sender, RoutedEventArgs e)
    {
        CurPos -= TimeSpan.FromSeconds(10);
    }

    private void ChapterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var container = sender as ComboBox;
        if (container == null || container.SelectedItem is not Demuxer.Chapter chapter) return;

        if (ChapterCombo.SelectedIndex == ChapterCombo.Items.IndexOf(AudiobookViewModel.Audiobook.CurChapter)) return;

        CurPos = TimeSpan.FromMilliseconds(chapter.StartTime);
    }

    private void PlaybackSpeedSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        Settings.PlaybackSpeed = AudiobookViewModel.Audiobook.PlaybackSpeed = e.NewValue;
        MediaPlayer.PlaybackRate = e.NewValue;
    }

    private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            Settings.Volume = AudiobookViewModel.Audiobook.Volume = e.NewValue;

            MediaPlayer.Volume = e.NewValue / 100;
            UpdateVolumeIcon();
        });
    }

    private void UpdateVolumeIcon()
    {
        AudiobookViewModel.Audiobook.VolumeLevelGlyph = AudiobookViewModel.Audiobook.Volume == 0 ? Audiobook.Volume0 :
            AudiobookViewModel.Audiobook.Volume <= 33 ? Audiobook.Volume1 :
            AudiobookViewModel.Audiobook.Volume <= 66 ? Audiobook.Volume2 : Audiobook.Volume3;
    }

    private void CompactViewButton_Click(object sender, RoutedEventArgs e)
    {
        AudiobookViewModel.Audiobook.IsCompact = true;
    }
}