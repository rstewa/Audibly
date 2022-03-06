using Audibly.Models;
using FlyleafLib;
using FlyleafLib.MediaPlayer;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.Diagnostics;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Audibly;

/// <summary>
///     An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    private Player Player;
    private Config Config;
    public Audiobook CurrentBook;

    public MainWindow()
    {
        InitializeComponent();

        Engine.Start(new EngineConfig()
        {
            UIRefresh = true,
            FFmpegPath = @"C:\Users\rstewa\source\repos\Audibly\FFmpeg"
        });

        // Create new config
        Config = new Config();

        // Initiliaze the player as Audio Player
        Config.Player.Usage = Usage.Audio;
        Player = new Player(Config);

        // Listen to property changed events
        Player.PropertyChanged += Player_PropertyChanged;
        Player.Audio.PropertyChanged += PlayerAudio_PropertyChanged;

        // Allow auto play on open
        Config.Player.AutoPlay = false;

        // Prepare Seek Offsets and Commands
        Config.Player.SeekOffset = TimeSpan.FromSeconds(10).Ticks;
        Config.Player.SeekOffset2 = TimeSpan.FromSeconds(30).Ticks;

        // play/pause button disabled until an audio file is successfully opened
        PlayPauseButton.IsEnabled = false;
        PreviousChapterButton.IsEnabled = false;
        SkipBack10Button.IsEnabled = false;
        SkipForward30Button.IsEnabled = false;
        NextChapterButton.IsEnabled = false;
    }

    private void PlayerAudio_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        ;
        //switch (e.PropertyName)
        //{
        //    case "Volume":
        //        volumeChangedFromLib = true;
        //        lblVolume.Text = Player.Audio.Volume.ToString() + "%";
        //        sliderVolume.Value = Player.Audio.Volume;
        //        volumeChangedFromLib = false;
        //        break;

        //    case "Mute":
        //        btnMute.Text = Player.Audio.Mute ? "Unmute" : "Mute";
        //        break;
        //}
    }

    private void Player_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case "CurTime":
                long curMs = Player.CurTime / 10000; // convert ticks to ms
                Utils.UI(() =>
                {
                    CurrentBook.UpdateCurrentChapter(curMs);
                    CurrentBook.CurrentTimeMs = curMs;
                });

                // if (Player.CurTime % 10000 == 0)
                // {
                //     long curMs = Player.CurTime / 10000; // convert ticks to ms
                //     CurrentBook.UpdateCurrentChapter(curMs);
                //     var t = TimeSpan.FromMilliseconds(curMs);
                //     CurrentBook.CurrentTime = $@"{(int) t.TotalHours}:{t:mm}:{t:ss}";
                // }

                break;

            //case "Duration":
            //    var duration = TimeSpan.FromTicks(Player.Duration);

            //    lblDuration.Text = duration.ToString(@"hh\:mm\:ss");
            //    sliderCurTime.Maximum = (int)duration.TotalSeconds;

            //    break;

            //case "Status":
            //    btnPlayPause.Text = Player.IsPlaying ? "Pause" : "Play";

            //    if (!Player.IsLive && Player.HasEnded && chkRepeat.Checked)
            //        Player.Seek(0);

            //    break;

            case "CanPlay":
                PlayPauseButton.IsEnabled = Player.CanPlay;
                PreviousChapterButton.IsEnabled = Player.CanPlay;
                SkipBack10Button.IsEnabled = Player.CanPlay;
                SkipForward30Button.IsEnabled = Player.CanPlay;
                NextChapterButton.IsEnabled = Player.CanPlay;
                break;
        }
    }

    private async void OpenAudiobook_Click(object sender, RoutedEventArgs e)
    {
        // Create the file picker
        var filePicker = new FileOpenPicker();

        // Get the current window's HWND by passing in the Window object
        var hwnd = WindowNative.GetWindowHandle(this);

        // Associate the HWND with the file picker
        InitializeWithWindow.Initialize(filePicker, hwnd);

        // Use file picker like normal!
        filePicker.FileTypeFilter.Add(".m4a");
        var file = await filePicker.PickSingleFileAsync();

        Debug.WriteLine($"[Class=MainWindow][Method=SetLocalMedia] file name: {file.Name}");

        CurrentBook = new Audiobook(file.Path);

        Player.OpenAsync(file.Path);
    }

    private void PreviousChapterButton_Click(object sender, RoutedEventArgs e) { }

    private void SkipBack10Button_Click(object sender, RoutedEventArgs e)
    {
        Player.SeekBackward();
    }

    private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
    {
        Player.TogglePlayPause();

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

    private void SkipForward30Button_Click(object sender, RoutedEventArgs e)
    {
        Player.SeekForward2();
    }

    private void NextChapterButton_Click(object sender, RoutedEventArgs e) { }

    private void SettingButton_Click(object sender, RoutedEventArgs e) { }
}