// Author: rstewa · https://github.com/rstewa
// Created: 3/21/2024
// Updated: 3/22/2024

using System;
using Audibly.App.ViewModels;
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
        throw new NotImplementedException();
    }

    private void PreviousChapterButton_Click(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void ChapterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // todo
        ;
    }

    private void NextChapterButton_Click(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void SkipBack10Button_Click(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void SkipForward30Button_Click(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void OpenAudiobook_Click(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void PlaybackSpeedSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            PlayerViewModel.NowPlaying.PlaybackSpeed = e.NewValue;
            PlayerViewModel.MediaPlayer.PlaybackRate = e.NewValue;
        });
    }

    private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        DispatcherQueue.EnqueueAsync(async () =>
        {
            PlayerViewModel.NowPlaying.Volume = e.NewValue;
            PlayerViewModel.MediaPlayer.Volume = e.NewValue / 100;
            await PlayerViewModel.NowPlaying.SaveAsync();
        });
    }
}