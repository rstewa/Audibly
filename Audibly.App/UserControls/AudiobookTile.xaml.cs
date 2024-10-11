// Author: rstewa · https://github.com/rstewa
// Created: 04/15/2024
// Updated: 10/11/2024

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Audibly.App.ViewModels;
using Audibly.Models;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
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

    public int SourcePathsCount => SourcePaths.Count;

    public List<SourceFile> SourcePaths
    {
        get => (List<SourceFile>)GetValue(SourcePathsProperty);
        set => SetValue(SourcePathsProperty, value);
    }

    public static readonly DependencyProperty SourcePathsProperty =
        DependencyProperty.Register(nameof(SourcePaths), typeof(List<SourceFile>), typeof(AudiobookTile),
            new PropertyMetadata(null));

    public string FilePath
    {
        get => (string)GetValue(FilePathProperty);
        set => SetValue(FilePathProperty, value);
    }

    public static readonly DependencyProperty FilePathProperty =
        DependencyProperty.Register(nameof(FilePath), typeof(string), typeof(AudiobookTile),
            new PropertyMetadata(null));

    public AudiobookTile()
    {
        InitializeComponent();
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
        await _dispatcherQueue.EnqueueAsync(async () =>
        {
            var audiobook = App.ViewModel.Audiobooks.FirstOrDefault(a => a.Title == Title);
            if (audiobook == null)
                return;

            PlayerViewModel.OpenAudiobook(audiobook);
        });
    }

    private void InfoButton_OnClick(object sender, RoutedEventArgs e)
    {
        // todo
    }

    private void ShowInFileExplorer_OnClick(object sender, RoutedEventArgs e)
    {
        Process p = new();
        p.StartInfo.FileName = "explorer.exe";
        p.StartInfo.Arguments = $"/select, \"{FilePath}\"";
        p.Start();
    }

    private async void DeleteAudiobook_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedAudiobook = App.ViewModel.Audiobooks.FirstOrDefault(a => a.Title == Title);
        await ViewModel.DeleteAudiobookAsync();
    }

    // TODO
    private void OnElementClicked(object sender, RoutedEventArgs e)
    {
        ;
    }

    private void ButtonTile_OnRightTapped(object sender, RightTappedRoutedEventArgs? e)
    {
        if (e is null) return;
        var myOption = new FlyoutShowOptions
        {
            ShowMode = FlyoutShowMode.Transient
        };
        CommandBarFlyout.ShowAt(ButtonTile, myOption);
    }

    private void OpenInAppFolder_OnClick(object sender, RoutedEventArgs e)
    {
        var selectedAudiobook = App.ViewModel.Audiobooks.FirstOrDefault(a => a.Title == Title);
        if (selectedAudiobook == null) return;
        var dir = Path.GetDirectoryName(selectedAudiobook.CoverImagePath);
        if (dir == null) return;
        Process p = new();
        p.StartInfo.FileName = "explorer.exe";
        p.StartInfo.Arguments = $"/open, \"{dir}\"";
        p.Start();
    }
}