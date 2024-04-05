// Author: rstewa · https://github.com/rstewa
// Created: 3/27/2024
// Updated: 3/28/2024

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Core;
using Audibly.App.Extensions;
using Audibly.App.ViewModels;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
// using CommunityToolkit.WinUI;

namespace Audibly.App.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class LibraryCardPage : Page
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

    public LibraryCardPage()
    {
        InitializeComponent();

#if DEBUG
        DeleteButton.Visibility = Visibility.Visible;
#endif
    }

    private async void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        await _dispatcherQueue.EnqueueAsync(() =>
        {
            if (PlayerViewModel.NowPlaying != null)
                PlayerViewModel.NowPlaying.IsNowPlaying = false;

            PlayerViewModel.NowPlaying = ViewModel.SelectedAudiobook;

            if (PlayerViewModel.NowPlaying == null) return;

            PlayerViewModel.NowPlaying.IsNowPlaying = true;
            PlayerViewModel.MediaPlayer.Source = MediaSource.CreateFromUri(PlayerViewModel.NowPlaying.FilePath.AsUri());
        });
    }

    private void LibraryCardView_OnItemClick(object sender, ItemClickEventArgs e)
    {
        ViewModel.SelectedAudiobook = (AudiobookViewModel)e.ClickedItem;
    }
}