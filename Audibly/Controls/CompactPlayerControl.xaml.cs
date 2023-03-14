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

    private TimeSpan CurPos
    {
        get => MediaPlayer.PlaybackSession.Position;
        set => MediaPlayer.PlaybackSession.Position = value < TimeSpan.Zero ? TimeSpan.Zero : value;
    }

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
        get => _localSettings.Values[TimePlayerWasPausedSettingLabel] as DateTime?;
        set => _localSettings.Values[TimePlayerWasPausedSettingLabel] = value;
    }

    private DateTime TimePlaybackPaused { get; set; }

    private MediaPlayer MediaPlayer => AudiobookViewModel.Audiobook.MediaPlayer;

    public CompactPlayerControl()
    {
        InitializeComponent();

        _localSettings = ApplicationData.Current.LocalSettings;

#if DEBUG
        // _localSettings.Values.Clear();
#endif

        // setting MediaPlayer properties
        AudioPlayerElement.SetMediaPlayer(AudiobookViewModel.Audiobook.MediaPlayer);
        MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;

        // hover over events
        CompactPlayerGrid.PointerEntered += CompactPlayerGridOnPointerEntered;
        CompactPlayerGrid.PointerExited += CompactPlayerGridOnPointerExited;
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

    private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
    {
        switch (sender.PlaybackState)
        {
            case MediaPlaybackState.Playing:
                DispatcherQueue.TryEnqueue(() =>
                {
                    // rewind current playback position 30 sec if book was paused for >= 10 minutes
                    // if (SmartRewind && _canSmartRewind && DateTime.UtcNow.Subtract(TimePlaybackPaused).TotalMinutes >=
                    //     SmartRewindDuration)
                    // {
                    //     CurPos -= TimeSpan.FromSeconds(30);
                    //     _canSmartRewind = false;
                    // }

                    if ((string)PlayPauseButton.Tag == "pause") return;
                    PlayPauseButton.Tag = "pause";
                    PlayPauseIcon.Symbol = Symbol.Pause;
                });

                break;

            case MediaPlaybackState.Paused:
                // grab current time for smart rewind feature
                // if (SmartRewind)
                // {
                //     TimePlaybackPaused = DateTime.UtcNow;
                //     _canSmartRewind = true;
                // }

                DispatcherQueue.TryEnqueue(() =>
                {
                    if ((string)PlayPauseButton.Tag == "play") return;
                    PlayPauseButton.Tag = "play";
                    PlayPauseIcon.Symbol = Symbol.Play;
                });

                break;
        }
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

    private void SkipForward30Button_Click(object sender, RoutedEventArgs e)
    {
        CurPos += TimeSpan.FromSeconds(30);
    }

    private void SkipBack10Button_Click(object sender, RoutedEventArgs e)
    {
        CurPos -= TimeSpan.FromSeconds(10);
    }

    private void DefaultViewButton_OnClick(object sender, RoutedEventArgs e)
    {
        AudiobookViewModel.Audiobook.IsCompact = false;
    }
}