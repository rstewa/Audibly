// Author: rstewa · https://github.com/rstewa
// Created: 3/21/2024
// Updated: 3/22/2024

using System;
using Audibly.App.ViewModels;
using Audibly.Models;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Audibly.App.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class LegacyPlayerPage : Page
{
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    /// <summary>
    ///     Gets the app-wide ViewModel instance.
    /// </summary>
    public MainViewModel ViewModel => App.ViewModel;

    /// <summary>
    ///     Gets the app-wide PlayerViewModel instance.
    /// </summary>
    public PlayerViewModel PlayerViewModel => App.PlayerViewModel;

    public LegacyPlayerPage()
    {
        InitializeComponent();
    }

    private void CompactViewButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO
        throw new NotImplementedException();
    }

    private void PreviousChapterButton_Click(object sender, RoutedEventArgs e)
    {
        PlayerViewModel.PreviousChapter();
    }

    private void NextChapterButton_Click(object sender, RoutedEventArgs e)
    {
        PlayerViewModel.NextChapter();
    }
    
    private void ChapterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var container = sender as ComboBox;
        if (container == null || container.SelectedItem is not ChapterInfo chapter) return;

        if (ChapterCombo.SelectedIndex == ChapterCombo.Items.IndexOf(PlayerViewModel.NowPlaying?.CurrentChapter)) return;

        PlayerViewModel.CurrentPosition = TimeSpan.FromMilliseconds(chapter.StartTime);
    }

    private void SkipBackButton_Click(object sender, RoutedEventArgs e)
    {
        PlayerViewModel.SkipBackward();
    }

    private void SkipForwardButton_Click(object sender, RoutedEventArgs e)
    {
        PlayerViewModel.SkipForward();
    }

    private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
    {
        if (PlayerViewModel.PlayPauseIcon == Symbol.Play)
            PlayerViewModel.MediaPlayer.Play();
        else
            PlayerViewModel.MediaPlayer.Pause();
    }

    private void OpenAudiobook_Click(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void PlaybackSpeedSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        var slider = sender as Slider;
        if (slider == null || !IsLoaded) return;

        PlayerViewModel.UpdatePlaybackSpeed(slider.Value);
    }

    private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        var slider = sender as Slider;
        if (slider == null || !IsLoaded) return;

        PlayerViewModel.UpdateVolume(slider.Value);
    }
}