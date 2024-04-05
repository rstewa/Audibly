// Author: rstewa · https://github.com/rstewa
// Created: 3/29/2024
// Updated: 4/2/2024

using System.Linq;
using Windows.Media.Core;
using Audibly.App.Extensions;
using Audibly.App.ViewModels;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Audibly.App.UserControls;

public sealed partial class AudiobookTile : UserControl
{
    /// <summary>
    ///     Gets the app-wide PlayerViewModel instance.
    /// </summary>
    public PlayerViewModel PlayerViewModel => App.PlayerViewModel;

    /// <summary>
    ///     Gets the app-wide ViewModel instance.
    /// </summary>
    public MainViewModel ViewModel => App.ViewModel;

    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(AudiobookTile), new PropertyMetadata(null));

    public string Author
    {
        get => (string)GetValue(AuthorProperty);
        set => SetValue(AuthorProperty, value);
    }

    public static readonly DependencyProperty AuthorProperty =
        DependencyProperty.Register(nameof(Author), typeof(string), typeof(AudiobookTile), new PropertyMetadata(null));

    public object Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register(nameof(Source), typeof(object), typeof(AudiobookTile), new PropertyMetadata(null));

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public static readonly DependencyProperty ProgressProperty =
        DependencyProperty.Register(nameof(Progress), typeof(double), typeof(AudiobookTile),
            new PropertyMetadata(0.0, ProgressPropertyChangedCallback));

    private static void ProgressPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not AudiobookTile audiobookTile) return;
        audiobookTile.ProgressGrid.Visibility = (double)e.NewValue < 1 ? Visibility.Collapsed : Visibility.Visible;
        // audiobookTile.ProgressTextBlock.Text = $"{(double)e.NewValue:P0}";
    }

    public AudiobookTile()
    {
        InitializeComponent();
    }

    private void Audiobook_OnClick(object sender, RoutedEventArgs e)
    {
        App.ViewModel.SelectedAudiobook = App.ViewModel.Audiobooks.FirstOrDefault(a => a.Title == Title);
    }

    private void AudiobookTile_OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        BlackOverlayGrid.Visibility = Visibility.Visible;
    }

    private void AudiobookTile_OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        BlackOverlayGrid.Visibility = Visibility.Collapsed;
    }

    private async void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        await _dispatcherQueue.EnqueueAsync(() =>
        {
            if (PlayerViewModel.NowPlaying != null)
                PlayerViewModel.NowPlaying.IsNowPlaying = false;

            ViewModel.SelectedAudiobook = App.ViewModel.Audiobooks.FirstOrDefault(a => a.Title == Title);

            PlayerViewModel.NowPlaying = ViewModel.SelectedAudiobook;

            if (PlayerViewModel.NowPlaying == null) return;

            PlayerViewModel.NowPlaying.IsNowPlaying = true;
            PlayerViewModel.MediaPlayer.Source = MediaSource.CreateFromUri(PlayerViewModel.NowPlaying.FilePath.AsUri());
        });
    }
}