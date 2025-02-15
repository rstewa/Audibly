// Author: rstewa · https://github.com/rstewa
// Updated: 02/14/2025

using System;
using System.Threading.Tasks;
using Audibly.App.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Audibly.App.UserControls;

public sealed partial class TitleArtistStack : UserControl
{
    public static readonly DependencyProperty SpacingProperty = DependencyProperty.Register(
        nameof(Spacing), typeof(double), typeof(TitleArtistStack), new PropertyMetadata(4.0));

    public static readonly DependencyProperty TitleFontSizeProperty = DependencyProperty.Register(
        nameof(TitleFontSize), typeof(double), typeof(TitleArtistStack), new PropertyMetadata(16.0));

    public static readonly DependencyProperty ArtistFontSizeProperty = DependencyProperty.Register(
        nameof(ArtistFontSize), typeof(double), typeof(TitleArtistStack), new PropertyMetadata(14.0));

    public static readonly DependencyProperty TitleMaxWidthProperty = DependencyProperty.Register(
        nameof(TitleMaxWidth), typeof(double), typeof(TitleArtistStack), new PropertyMetadata(230.0));

    public static readonly DependencyProperty ShowChapterTitleProperty = DependencyProperty.Register(
        nameof(ShowChapterTitle), typeof(bool), typeof(TitleArtistStack), new PropertyMetadata(false));

    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    private bool _isPointerOver;

    public TitleArtistStack()
    {
        InitializeComponent();
        TitleMarqueeText.MarqueeCompleted += TitleMarqueeText_MarqueeCompleted;
        CurrentChapterTitleMarqueeText.MarqueeCompleted += CurrentChapterTitleMarqueeText_MarqueeCompleted;
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

    public bool ShowChapterTitle
    {
        get => (bool)GetValue(ShowChapterTitleProperty);
        set => SetValue(ShowChapterTitleProperty, value);
    }

    private async void TitleMarqueeText_MarqueeCompleted(object? sender, EventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() => TitleMarqueeText.StopMarquee());
        await Task.Delay(TimeSpan.FromSeconds(5)); // wait for 3 seconds
        _dispatcherQueue.TryEnqueue(() => TitleMarqueeText.StartMarquee());
    }

    private void CurrentChapterTitleMarqueeText_MarqueeCompleted(object? sender, EventArgs e)
    {
        if (!_isPointerOver) _dispatcherQueue.TryEnqueue(() => CurrentChapterTitleMarqueeText.StopMarquee());
    }

    private void CurrentChapterTitleTextBlock_OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() => CurrentChapterTitleMarqueeText.StartMarquee());
        _isPointerOver = true;
    }

    private void CurrentChapterTitleMarqueeText_OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        _isPointerOver = false;
        // _dispatcherQueue.TryEnqueue(() => CurrentChapterTitleMarqueeText.StopMarquee());
    }
}