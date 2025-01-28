// Author: rstewa · https://github.com/rstewa
// Updated: 01/26/2025

using System;
using Audibly.App.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Audibly.App.UserControls;

public sealed partial class NowPlayingBar : UserControl
{
    public NowPlayingBar()
    {
        InitializeComponent();
    }

    /// <summary>
    ///     Gets the app-wide PlayerViewModel instance.
    /// </summary>
    public PlayerViewModel PlayerViewModel => App.PlayerViewModel;

    private async void NowPlayingBar_OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        var slider = sender as Slider;

        if (slider == null || slider.Value == 0) return;

        if (PlayerViewModel.NowPlaying?.CurrentChapter == null) return;

        PlayerViewModel.CurrentPosition =
            TimeSpan.FromMilliseconds(PlayerViewModel.NowPlaying.CurrentChapter.StartTime + slider.Value);

        await PlayerViewModel.NowPlaying.SaveAsync();
    }
}