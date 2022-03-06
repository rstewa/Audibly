using System;
using System.ComponentModel;
using System.IO;
using Windows.Storage.Pickers;
using Audibly.Model;
using FlyleafLib;
using FlyleafLib.MediaPlayer;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT.Interop;

namespace Audibly;

public sealed partial class MainWindow : Window
{
    private readonly Player _player;
    private bool _lockUpdate;

    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new AudiobookViewModel();

        Engine.Start(
            new EngineConfig
            {
                UIRefresh = true,
                FFmpegPath = $"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FFMpeg")}"
            }
        );

        // Create new config
        var config = new Config { Player = { Usage = Usage.Audio } };
        _player = new Player(config);

        // Listen to property changed events
        _player.PropertyChanged += Player_PropertyChanged;
        _player.Audio.PropertyChanged += PlayerAudio_PropertyChanged;

        // Allow auto play on open
        config.Player.AutoPlay = false;

        // Prepare Seek Offsets and Commands
        config.Player.SeekOffset = TimeSpan.FromSeconds(10).Ticks;
        config.Player.SeekOffset2 = TimeSpan.FromSeconds(30).Ticks;

        // play/pause button disabled until an audio file is successfully opened
        ToggleAudioControls(false);
    }

    public AudiobookViewModel ViewModel { get; set; }

    private void ToggleAudioControls(bool isEnabled)
    {
        PlayPauseButton.IsEnabled = isEnabled;
        PreviousChapterButton.IsEnabled = isEnabled;
        SkipBack10Button.IsEnabled = isEnabled;
        SkipForward30Button.IsEnabled = isEnabled;
        NextChapterButton.IsEnabled = isEnabled;

        CurrentTime_TextBlock.Opacity = ChapterProgress_ProgressBar.Opacity =
            CurrentChapterDuration_TextBlock.Opacity = isEnabled ? 1.0 : 0.5;
    }

    private void PlayerAudio_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case "Volume":
                // volumeChangedFromLib = true;
                // lblVolume.Text = Player.Audio.Volume.ToString() + "%";
                // sliderVolume.Value = Player.Audio.Volume;
                // volumeChangedFromLib = false;
                break;

            case "Mute":
                // btnMute.Text = Player.Audio.Mute ? "Unmute" : "Mute";
                break;
        }
    }

    private void Player_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case "CurTime":
                if (_lockUpdate) return;
                ViewModel.Audiobook.Update(_player.CurTime.ToMs());
                break;

            case "Duration": break;

            case "Status": break;

            case "CanPlay":
                ToggleAudioControls(true);
                break;
        }
    }

    private async void OpenAudiobook_Click(object sender, RoutedEventArgs e)
    {
        var filePicker = new FileOpenPicker();
        var hwnd = WindowNative.GetWindowHandle(this);
        InitializeWithWindow.Initialize(filePicker, hwnd);
        filePicker.FileTypeFilter.Add(".m4a");
        filePicker.FileTypeFilter.Add(".m4b");
        var file = await filePicker.PickSingleFileAsync();
        if (file == null) return;

        ViewModel.Audiobook.Init(file.Path);
        _player.OpenAsync(file.Path);
    }

    private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
    {
        _player.TogglePlayPause();

        if ((string)PlayPauseButton.Tag == "play")
        {
            PlayPauseButton.Icon = new SymbolIcon(Symbol.Pause);
            PlayPauseButton.Label = "Pause";
            PlayPauseButton.Tag = "pause";
        }
        else
        {
            PlayPauseButton.Icon = new SymbolIcon(Symbol.Play);
            PlayPauseButton.Label = "Play";
            PlayPauseButton.Tag = "play";
        }
    }

    private void NextChapterButton_Click(object sender, RoutedEventArgs e)
    {
        _lockUpdate = true;
        _player.SeekAccurate(ViewModel.Audiobook.GetNextChapter());
        _lockUpdate = false;
    }

    private void PreviousChapterButton_Click(object sender, RoutedEventArgs e)
    {
        _lockUpdate = true;
        _player.SeekAccurate(ViewModel.Audiobook.GetPrevChapter());
        _lockUpdate = false;
    }

    private void SkipForward30Button_Click(object sender, RoutedEventArgs e)
    {
        _player.SeekForward2();
    }

    private void SkipBack10Button_Click(object sender, RoutedEventArgs e)
    {
        _player.SeekBackward();
    }

    private void SettingButton_Click(object sender, RoutedEventArgs e)
    {
    }
}