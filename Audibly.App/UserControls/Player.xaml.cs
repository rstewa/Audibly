// Author: rstewa · https://github.com/rstewa
// Created: 3/11/2024
// Updated: 3/13/2024

using System;
using Windows.Media.Playback;
using Audibly.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Audibly.App.UserControls;

public sealed partial class Player : UserControl
{
    /// <summary>
    ///     Gets the app-wide ViewModel instance.
    /// </summary>
    public MainViewModel ViewModel => App.ViewModel;

    public Player()
    {
        InitializeComponent();
        AudioPlayer.SetMediaPlayer(ViewModel.mediaPlayer);
        InitializeAudioPlayer();
    }

    private void InitializeAudioPlayer()
    {
        ViewModel.mediaPlayer.AutoPlay = false;
        ViewModel.mediaPlayer.AudioCategory = MediaPlayerAudioCategory.Media;
        ViewModel.mediaPlayer.AudioDeviceType = MediaPlayerAudioDeviceType.Multimedia;
        ViewModel.mediaPlayer.CommandManager.IsEnabled = true; // todo: what is this?
        ViewModel.mediaPlayer.MediaOpened += AudioPlayer_MediaOpened;
        ViewModel.mediaPlayer.MediaEnded += AudioPlayer_MediaEnded;
        ViewModel.mediaPlayer.MediaFailed += AudioPlayer_MediaFailed;
        ViewModel.mediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
        ViewModel.mediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
    }

    private bool isDragging;

    private void AudioPlayer_MediaOpened(MediaPlayer sender, object args)
    {
        NowPlayingBar.Maximum = AudioPlayer.MediaPlayer.PlaybackSession.NaturalDuration.TotalSeconds;
    }

    private void AudioPlayer_MediaEnded(MediaPlayer sender, object args)
    {
        NowPlayingBar.Value = 0;
    }

    private void AudioPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        throw new NotImplementedException();
    }

    private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
    {
        throw new NotImplementedException();
    }

    private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
    {
        throw new NotImplementedException();
    }

    private void NowPlayingBar_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (isDragging) AudioPlayer.MediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(e.NewValue);
    }

    private void NowPlayingBar_OnDragStarting(UIElement sender, DragStartingEventArgs args)
    {
        isDragging = true;
    }

    private void NowPlayingBar_OnDropCompleted(UIElement sender, DropCompletedEventArgs args)
    {
        isDragging = false;
        AudioPlayer.MediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(NowPlayingBar.Value);
    }

    private void PreviousButton_OnClick(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void PlayPauseButton_OnClick(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void NextButton_OnClick(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }
}