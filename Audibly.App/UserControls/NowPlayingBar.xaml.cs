// Author: rstewa · https://github.com/rstewa
// Updated: 01/26/2025

using System;
using Audibly.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace Audibly.App.UserControls;

public sealed partial class NowPlayingBar : UserControl
{
    private bool _isPointerInteracting;
    private double _pendingSeekValue;

    public NowPlayingBar()
    {
        InitializeComponent();

        ProgressSlider.AddHandler(PointerPressedEvent,
            new PointerEventHandler(Slider_OnPointerPressed), true);
        ProgressSlider.AddHandler(PointerReleasedEvent,
            new PointerEventHandler(Slider_OnPointerReleased), true);
        ProgressSlider.AddHandler(PointerCanceledEvent,
            new PointerEventHandler(Slider_OnPointerReleased), true);
    }

    /// <summary>
    ///     Gets the app-wide PlayerViewModel instance.
    /// </summary>
    public PlayerViewModel PlayerViewModel => App.PlayerViewModel;

    private void Slider_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isPointerInteracting = true;
        PlayerViewModel.IsUserSeeking = true;
    }

    private async void Slider_OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_isPointerInteracting) return;
        _isPointerInteracting = false;

        await PlayerViewModel.SeekToPositionAsync(_pendingSeekValue);
    }

    private async void Slider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        _pendingSeekValue = e.NewValue;

        // If not dragging (e.g. a tap/click to seek), commit immediately
        if (!_isPointerInteracting && PlayerViewModel.IsUserSeeking)
        {
            await PlayerViewModel.SeekToPositionAsync(e.NewValue);
        }
    }
}