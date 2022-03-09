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
using Windows.Storage;

namespace Audibly;

public sealed partial class MainWindow : Window
{
    private readonly Player _player;
    private bool _lockUpdate;
    private ApplicationDataContainer _localSettings;

    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new AudiobookViewModel();
        _localSettings = ApplicationData.Current.LocalSettings;

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

        _player.OpenCompleted += (o, e) =>
        {
            Utils.UI(() => 
            { 
                if (_localSettings.Values["currentTime"] == null)
                {
                    _localSettings.Values["currentTime"] = 0;
                    _player.CurTime = 0;
                }
                else
                {
                    var currentTime = Convert.ToInt32(_localSettings.Values["currentTime"]);
                    _player.CurTime = TimeSpan.FromMilliseconds(currentTime).Ticks;
                }
                // ViewModel.Audiobook.Update(_player.CurTime.ToMs());
            });
        };

        // play/pause button disabled until an audio file is successfully opened
        ToggleAudioControls(false);
        if (_localSettings.Values["currentAudiobookPath"] != null)
        {
            var currentAudiobookPath = _localSettings.Values["currentAudiobookPath"].ToString();
            ViewModel.Audiobook.Init(currentAudiobookPath);
            _player.OpenAsync(currentAudiobookPath);
        }
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
                SaveProgress();
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

        _localSettings.Values["currentAudiobookPath"] = file.Path;
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
        _player.CurTime = ViewModel.Audiobook.GetNextChapter();
        // ViewModel.Audiobook.Update(_player.CurTime.ToMs());
        _lockUpdate = false;
    }

    private void PreviousChapterButton_Click(object sender, RoutedEventArgs e)
    {
        _lockUpdate = true;
        _player.CurTime = ViewModel.Audiobook.GetPrevChapter(_player.CurTime.ToMs());
        // ViewModel.Audiobook.Update(_player.CurTime.ToMs());
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

    private void SaveProgress()
    {
        _localSettings.Values["currentTime"] = _player.CurTime.ToMs();
    }
}