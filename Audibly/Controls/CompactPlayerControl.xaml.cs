// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

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
using Microsoft.UI.Xaml.Input;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Audibly.Controls;

public sealed partial class CompactPlayerControl : UserControl
{
    private readonly ApplicationDataContainer _localSettings;
    private string _curBookName;
    private const double SmartRewindDuration = 0.5; // 0.0;
    private const bool SmartRewind = true;

    private bool _canSmartRewind { get; set; } = false;

    private string CurAudiobookPathSettingValue
    {
        get => _localSettings.Values["currentAudiobookPath"]?.ToString();
        set => _localSettings.Values["currentAudiobookPath"] = value;
    }

    private string CurPosSettingLabel => $"{_curBookName}:CurrentPosition";

    private double? CurPosSettingValue
    {
        get => _localSettings.Values[CurPosSettingLabel]?.ToDouble();
        set => _localSettings.Values[CurPosSettingLabel] = value?.ToString(CultureInfo.InvariantCulture);
    }

    // private TimeSpan CurPos
    // {
    //     get => MediaPlayer.PlaybackSession.Position;
    //     set => MediaPlayer.PlaybackSession.Position = value < TimeSpan.Zero ? TimeSpan.Zero : value;
    // }

    private string VolumeSettingLabel => $"{_curBookName}:Volume";

    private double? VolumeSettingValue
    {
        get => _localSettings.Values[VolumeSettingLabel]?.ToDouble();
        set => _localSettings.Values[VolumeSettingLabel] = value?.ToString();
    }

    private double Volume
    {
        get => AudiobookViewModel.Audiobook.Volume;
        set => AudiobookViewModel.Audiobook.Volume = value;
    }

    private string TimePlayerWasPausedSettingLabel => $"{_curBookName}:TimePlayerWasPaused";

    private DateTime? TimePlayerWasPaused
    {
        // get
        // {
        //     DateTime.TryParse(_localSettings.Values[TimePlayerWasPausedSettingLabel]?.ToString(), out var result);
        //     return result;
        // }
        get => _localSettings.Values[TimePlayerWasPausedSettingLabel] as DateTime?;
        set => _localSettings.Values[TimePlayerWasPausedSettingLabel] = value;
    }

    private DateTime TimePlaybackPaused { get; set; }

    // private MediaPlayer MediaPlayer
    // {
    //     get
    //     {
    //         if (AudioPlayerElement.MediaPlayer == null) AudioPlayerElement.SetMediaPlayer(new MediaPlayer());
    //         return AudioPlayerElement.MediaPlayer;
    //     }
    // }

    public CompactPlayerControl()
    {
        InitializeComponent();
        
        _localSettings = ApplicationData.Current.LocalSettings;

#if DEBUG
        // _localSettings.Values.Clear();
#endif

        // setting MediaPlayer properties
        // MediaPlayer.AutoPlay = false;
        // MediaPlayer.AudioCategory = MediaPlayerAudioCategory.Media;
        // MediaPlayer.AudioDeviceType = MediaPlayerAudioDeviceType.Multimedia;
        // MediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
        // MediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
        // MediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
        // MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
        
        // hover over events
        CompactPlayerGrid.PointerEntered += CompactPlayerGridOnPointerEntered;
        CompactPlayerGrid.PointerExited += CompactPlayerGridOnPointerExited;

        // disable buttons until a book is opened
        ToggleAudioControls(false);

        // means an audiobook wasn't open when the application last closed and/or its the 1st time the application has been run
        if (CurAudiobookPathSettingValue == null) return;

        // gets and/or sets the current audiobooks metadata and viewmodel
        AudiobookViewModel.Audiobook.Init(CurAudiobookPathSettingValue);

        // I'm sure there's a better way to do this ...
        var file = StorageFile.GetFileFromPathAsync(CurAudiobookPathSettingValue).GetAwaiter().GetResult();

        MediaPlayerElement_Init(file);
    }

    private void CompactPlayerGridOnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        PlayerControls_Grid.Visibility = Visibility.Collapsed;
        BlackOverlay_Canvas.Visibility = Visibility.Collapsed;
    }

    private void CompactPlayerGridOnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        PlayerControls_Grid.Visibility = Visibility.Visible;
        BlackOverlay_Canvas.Visibility = Visibility.Visible;
    }

    //     private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
//     {
//         switch (sender.PlaybackState)
//         {
//             case MediaPlaybackState.Playing:
//                 DispatcherQueue.TryEnqueue(() =>
//                 {
//                     // rewind current playback position 30 sec if book was paused for >= 10 minutes
//                     if (SmartRewind && _canSmartRewind && DateTime.UtcNow.Subtract(TimePlaybackPaused).TotalMinutes >=
//                         SmartRewindDuration)
//                     {
//                         CurPos -= TimeSpan.FromSeconds(30);
//                         _canSmartRewind = false;
//                     }
//
//                     if ((string)PlayPauseButton.Tag == "pause") return;
//                     PlayPauseButton.Tag = "pause";
//                     PlayPauseIcon.Symbol = Symbol.Pause;
//                 });
//
//                 break;
//
//             case MediaPlaybackState.Paused:
//                 // grab current time for smart rewind feature
//                 if (SmartRewind)
//                 {
//                     TimePlaybackPaused = DateTime.UtcNow;
//                     _canSmartRewind = true;
//                 }
//
//                 DispatcherQueue.TryEnqueue(() =>
//                 {
//                     if ((string)PlayPauseButton.Tag == "play") return;
//                     PlayPauseButton.Tag = "play";
//                     PlayPauseIcon.Symbol = Symbol.Play;
//                 });
//
//                 break;
//         }
//     }
//
//     private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
//     {
//         DispatcherQueue.TryEnqueue(() =>
//         {
//             // this is here to handle an edge case where a SmartRewind should happen but
//             // the user has changed the players current playback position before hitting
//             // play again
//             if (MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Paused)
//                 _canSmartRewind = false;
//
//             AudiobookViewModel.Audiobook.Update(MediaPlayer.PlaybackSession.Position.TotalMilliseconds);
//             ChapterCombo.SelectedIndex = ChapterCombo.Items.IndexOf(AudiobookViewModel.Audiobook.CurChapter);
//             CurPosSettingValue = CurPos.TotalMilliseconds;
//         });
//     }
//
//     private async void OpenAudiobook_Click(object sender, RoutedEventArgs e)
//     {
//         var picker = new FileOpenPicker();
// #if !WINDOWS_UWP
//         InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
// #endif
//         picker.ViewMode = PickerViewMode.Thumbnail;
//         picker.SuggestedStartLocation = PickerLocationId.Desktop;
//         picker.FileTypeFilter.Add(".m4b");
//
//         var file = await picker.PickSingleFileAsync()!;
//         if (file is null) return;
//
//         CurAudiobookPathSettingValue = file.Path;
//         AudiobookViewModel.Audiobook.Init(file.Path);
//
//         MediaPlayerElement_Init(file);
//     }

    private void MediaPlayerElement_Init(IStorageFile file)
    {
        // DispatcherQueue.TryEnqueue(() =>
        // {
        //     // TODO: the following 2 properties should probably be in the viewmodel
        //     MediaPlayer.Source = MediaSource.CreateFromStorageFile(file);
        //     ChapterCombo.SelectedIndex = ChapterCombo.Items.IndexOf(AudiobookViewModel.Audiobook.CurChapter);
        //
        //     ToggleAudioControls(true);
        // });
    }

    // private void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
    // {
    //     DispatcherQueue.TryEnqueue(() =>
    //     {
    //         _curBookName = Path.GetFileNameWithoutExtension(AudiobookViewModel.Audiobook.FilePath);
    //
    //         CurPosSettingValue ??= 0;
    //         CurPos = TimeSpan.FromMilliseconds(CurPosSettingValue ?? 0);
    //
    //         VolumeSettingValue ??= 100;
    //         Volume = VolumeSettingValue ?? 100;
    //         UpdateVolumeIcon();
    //     });
    // }
    //
    // private void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    // {
    //     // TODO: add info-bar error message
    //     ToggleAudioControls(false);
    // }

    private void ToggleAudioControls(bool isEnabled)
    {
        // DispatcherQueue.TryEnqueue(() =>
        // {
        //     PlayPauseButton.IsEnabled = isEnabled;
        //     PreviousChapterButton.IsEnabled = isEnabled;
        //     SkipBack10Button.IsEnabled = isEnabled;
        //     SkipForward30Button.IsEnabled = isEnabled;
        //     NextChapterButton.IsEnabled = isEnabled;
        //     ChapterCombo.IsEnabled = isEnabled;
        //     VolumeLevelButton.IsEnabled = isEnabled;
        //     PlaybackSpeedButton.IsEnabled = isEnabled;
        //
        //     CurrentTimeTextBlock.Opacity =
        //         ChapterProgressProgressBar.Opacity =
        //             CurrentChapterDurationTextBlock.Opacity = isEnabled ? 1.0 : 0.5;
        // });
    }

    private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
    {
        // DispatcherQueue.TryEnqueue(() =>
        // {
        //     if ((string)PlayPauseButton.Tag == "play")
        //         MediaPlayer.Play();
        //     else
        //         MediaPlayer.Pause();
        // });
    }

    // private void NextChapterButton_Click(object sender, RoutedEventArgs e)
    // {
    //     CurPos = AudiobookViewModel.Audiobook.GetNextChapter();
    //     ChapterCombo.SelectedIndex = ChapterCombo.Items.IndexOf(AudiobookViewModel.Audiobook.CurChapter);
    // }
    //
    // private void PreviousChapterButton_Click(object sender, RoutedEventArgs e)
    // {
    //     CurPos = AudiobookViewModel.Audiobook.GetPrevChapter(CurPos.TotalMilliseconds);
    //     ChapterCombo.SelectedIndex = ChapterCombo.Items.IndexOf(AudiobookViewModel.Audiobook.CurChapter);
    // }

    private void SkipForward30Button_Click(object sender, RoutedEventArgs e)
    {
        // CurPos += TimeSpan.FromSeconds(30);
    }

    private void SkipBack10Button_Click(object sender, RoutedEventArgs e)
    {
        // CurPos -= TimeSpan.FromSeconds(10);
    }

    // private void ChapterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    // {
    //     var container = sender as ComboBox;
    //     if (container == null || container.SelectedItem is not Demuxer.Chapter chapter) return;
    //
    //     if (ChapterCombo.SelectedIndex == ChapterCombo.Items.IndexOf(AudiobookViewModel.Audiobook.CurChapter)) return;
    //
    //     CurPos = TimeSpan.FromMilliseconds(chapter.StartTime);
    // }

    // private void PlaybackSpeedSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    // {
    //     // TODO: probably want to save this as a setting and then put the PlaybackSpeedSlider.Value in the ViewModel
    //     MediaPlayer.PlaybackRate = PlaybackSpeedSlider.Value;
    // }
    //
    // private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    // {
    //     DispatcherQueue.TryEnqueue(() =>
    //     {
    //         VolumeSettingValue = Volume = e.NewValue;
    //         MediaPlayer.Volume = e.NewValue / 100;
    //         UpdateVolumeIcon();
    //     });
    // }

    private void UpdateVolumeIcon()
    {
        AudiobookViewModel.Audiobook.VolumeLevelGlyph = Volume == 0 ? Audiobook.Volume0 :
            Volume <= 33 ? Audiobook.Volume1 :
            Volume <= 66 ? Audiobook.Volume2 : Audiobook.Volume3;
    }

    private void CompactViewButton_Click(object sender, RoutedEventArgs e)
    {
        AudiobookViewModel.Audiobook.IsCompact = true;
    }

    private void DefaultViewButton_OnClick(object sender, RoutedEventArgs e)
    {
        AudiobookViewModel.Audiobook.IsCompact = false;
    }
}