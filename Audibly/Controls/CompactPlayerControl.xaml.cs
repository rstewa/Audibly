//   Author: Ryan Stewart
//   Date: 03/13/2023

using System;
using Audibly.Model;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Media.Playback;
using Microsoft.UI.Xaml.Input;

namespace Audibly.Controls;

public sealed partial class CompactPlayerControl : UserControl
{
    private TimeSpan CurPos
    {
        get => MediaPlayer.PlaybackSession.Position;
        set => MediaPlayer.PlaybackSession.Position = value < TimeSpan.Zero ? TimeSpan.Zero : value;
    }

    private MediaPlayer MediaPlayer => AudiobookViewModel.Audiobook.MediaPlayer;

    public CompactPlayerControl()
    {
        InitializeComponent();

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