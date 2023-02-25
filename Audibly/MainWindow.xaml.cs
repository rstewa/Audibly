//   Author: Ryan Stewart
//   Date: 05/20/2022

using System;
using System.Globalization;
using System.IO;
using Windows.Storage;
using Windows.Storage.Pickers;
using Audibly.Extensions;
using Audibly.Model;
using FlyleafLib.MediaFramework.MediaDemuxer;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT.Interop;
using Windows.Media.Playback;
using Windows.Media.Core;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Audibly;

public sealed partial class MainWindow
{
    private readonly ApplicationDataContainer _localSettings;
    private string _curPosStg;
    
    private DateTime TimePlaybackPaused { get; set; }
    private const double SmartRewindDuration = 1.0;

    private const bool SmartRewind = true;

    private TimeSpan CurPos
    {
        get => MediaPlayer.PlaybackSession.Position;
        set => MediaPlayer.PlaybackSession.Position = value < TimeSpan.Zero ? TimeSpan.Zero : value;
    }

    private MediaPlayer MediaPlayer
    {
        get
        {
            if (AudioPlayerElement.MediaPlayer == null) AudioPlayerElement.SetMediaPlayer(new MediaPlayer());
            return AudioPlayerElement.MediaPlayer;
        }
    }

    public MainWindow()
    {
        InitializeComponent();
        this.SetWindowSize(315, 440, false, false, true, false);
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        ViewModel = new AudiobookViewModel();

        _localSettings = ApplicationData.Current.LocalSettings;
#if DEBUG
        // _localSettings.Values.Clear();
#endif

        MediaPlayer.AutoPlay = false;
        MediaPlayer.AudioCategory = MediaPlayerAudioCategory.Media;
        MediaPlayer.AudioDeviceType = MediaPlayerAudioDeviceType.Multimedia;
        MediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
        MediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
        MediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
        MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;

        ToggleAudioControls(false); // disable buttons until a book is opened

        CurrentTimeTextBlock.Opacity = 0.5;
        CurrentChapterDurationTextBlock.Opacity = 0.5;

        if (_localSettings.Values["currentAudiobookPath"] == null) return; // means a book was opened for the 1st time

        var currentAudiobookPath = _localSettings.Values["currentAudiobookPath"].ToString();
        ViewModel.Audiobook.Init(currentAudiobookPath);

        var file = StorageFile.GetFileFromPathAsync(currentAudiobookPath).GetAwaiter().GetResult(); // gross
        MediaPlayerElement_Init(file);
    }

    public AudiobookViewModel ViewModel { get; }

    private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
    {
        switch (sender.PlaybackState)
        {
            case MediaPlaybackState.Playing:
                DispatcherQueue.TryEnqueue(() =>
                {
                    // rewind current playback position 30 sec if book was paused for >= 10 minutes
                    if (SmartRewind && DateTime.UtcNow.Subtract(TimePlaybackPaused).TotalMinutes >= SmartRewindDuration)
                        CurPos -= TimeSpan.FromSeconds(30);

                    if ((string)PlayPauseButton.Tag == "pause") return;
                    PlayPauseButton.Tag = "pause";
                    PlayPauseIcon.Symbol = Symbol.Pause;
                });

                break;

            case MediaPlaybackState.Paused:
                // grab current time for smart rewind feature
                if (SmartRewind)
                    TimePlaybackPaused = DateTime.UtcNow;

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
            ViewModel.Audiobook.Update(MediaPlayer.PlaybackSession.Position.TotalMilliseconds);
            ChapterCombo.SelectedIndex = ChapterCombo.Items.IndexOf(ViewModel.Audiobook.CurChapter);
            SaveProgress();
        });
    }

    private async void OpenAudiobook_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
#if !WINDOWS_UWP
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
#endif
        picker.ViewMode = PickerViewMode.Thumbnail;
        picker.SuggestedStartLocation = PickerLocationId.Desktop;
        picker.FileTypeFilter.Add(".m4b");

        var file = await picker.PickSingleFileAsync()!;
        if (file is null) return;

        _localSettings.Values["currentAudiobookPath"] = file.Path;
        ViewModel.Audiobook.Init(file.Path);

        MediaPlayerElement_Init(file);
    }

    private void MediaPlayerElement_Init(IStorageFile file)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            MediaPlayer.Source = MediaSource.CreateFromStorageFile(file);
            ChapterCombo.SelectedIndex = ChapterCombo.Items.IndexOf(ViewModel.Audiobook.CurChapter);
            ToggleAudioControls(true);
        });
    }

    private void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            var curBookName = Path.GetFileNameWithoutExtension(ViewModel.Audiobook.FilePath);
            _curPosStg = _curPosStg != curBookName ? curBookName : _curPosStg;

            if (_localSettings.Values[_curPosStg!] == null)
            {
                _localSettings.Values[_curPosStg] = 0;
                CurPos = TimeSpan.FromSeconds(0);
            }
            else
            {
                CurPos = TimeSpan.FromMilliseconds(Convert.ToDouble(_localSettings.Values[_curPosStg!]));
            }
        });
    }

    private void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        // todo -> add info-bar error message
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

            CurrentTimeTextBlock.Opacity = ChapterProgressProgressBar.Opacity =
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
        CurPos = ViewModel.Audiobook.GetNextChapter();
        ChapterCombo.SelectedIndex = ChapterCombo.Items.IndexOf(ViewModel.Audiobook.CurChapter);
    }

    private void PreviousChapterButton_Click(object sender, RoutedEventArgs e)
    {
        CurPos = ViewModel.Audiobook.GetPrevChapter(CurPos.TotalMilliseconds);
        ChapterCombo.SelectedIndex = ChapterCombo.Items.IndexOf(ViewModel.Audiobook.CurChapter);
    }

    private void SkipForward30Button_Click(object sender, RoutedEventArgs e)
    {
        CurPos += TimeSpan.FromSeconds(30);
    }

    private void SkipBack10Button_Click(object sender, RoutedEventArgs e)
    {
        CurPos -= TimeSpan.FromSeconds(10);
    }

    private void SaveProgress()
    {
        _localSettings.Values[_curPosStg] =
            MediaPlayer.PlaybackSession.Position.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
    }

    private void ChapterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var container = sender as ComboBox;
        if (container == null || container.SelectedItem is not Demuxer.Chapter chapter) return;

        if (ChapterCombo.SelectedIndex == ChapterCombo.Items.IndexOf(ViewModel.Audiobook.CurChapter)) return;

        CurPos = TimeSpan.FromMilliseconds(chapter.StartTime);
    }

    private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        var volume = AudioLevelSlider.Value;

        DispatcherQueue.TryEnqueue(() =>
        {
            ViewModel.Audiobook.AudioLevelGlyph = volume == 0 ? Audiobook.Volume0 :
                volume <= 33 ? Audiobook.Volume1 :
                volume <= 66 ? Audiobook.Volume2 : Audiobook.Volume3;
            MediaPlayer.Volume = volume / 100;
        });
    }

    private void PlaybackSpeedSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        MediaPlayer.PlaybackRate = PlaybackSpeedSlider.Value;
    }
}