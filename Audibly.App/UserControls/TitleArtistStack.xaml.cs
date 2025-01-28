// Author: rstewa · https://github.com/rstewa
// Updated: 01/28/2025

using System;
using System.Threading.Tasks;
using Audibly.App.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.UserControls;

public sealed partial class TitleArtistStack : UserControl
{
    public static readonly DependencyProperty SpacingProperty = DependencyProperty.Register(
        nameof(Spacing), typeof(double), typeof(TitleArtistStack), new PropertyMetadata(0.0));

    public static readonly DependencyProperty TitleFontSizeProperty = DependencyProperty.Register(
        nameof(TitleFontSize), typeof(double), typeof(TitleArtistStack), new PropertyMetadata(16.0));

    public static readonly DependencyProperty ArtistFontSizeProperty = DependencyProperty.Register(
        nameof(ArtistFontSize), typeof(double), typeof(TitleArtistStack), new PropertyMetadata(14.0));

    public static readonly DependencyProperty TitleMaxWidthProperty = DependencyProperty.Register(
        nameof(TitleMaxWidth), typeof(double), typeof(TitleArtistStack), new PropertyMetadata(230.0));

    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    public TitleArtistStack()
    {
        InitializeComponent();
        TitleMarqueeText.MarqueeCompleted += TitleMarqueeText_MarqueeCompleted;
    }

    /// <summary>
    ///     Gets the app-wide PlayerViewModel instance.
    /// </summary>
    public PlayerViewModel PlayerViewModel => App.PlayerViewModel;

    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public double TitleFontSize
    {
        get => (double)GetValue(TitleFontSizeProperty);
        set => SetValue(TitleFontSizeProperty, value);
    }

    public double ArtistFontSize
    {
        get => (double)GetValue(ArtistFontSizeProperty);
        set => SetValue(ArtistFontSizeProperty, value);
    }

    public double TitleMaxWidth
    {
        get => (double)GetValue(TitleMaxWidthProperty);
        set => SetValue(TitleMaxWidthProperty, value);
    }

    private async void TitleMarqueeText_MarqueeCompleted(object? sender, EventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() => TitleMarqueeText.StopMarquee());
        await Task.Delay(TimeSpan.FromSeconds(3)); // wait for 3 seconds
        _dispatcherQueue.TryEnqueue(() => TitleMarqueeText.StartMarquee());
    }
}