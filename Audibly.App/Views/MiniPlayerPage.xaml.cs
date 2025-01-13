using System;
using Audibly.App.ViewModels;
using Audibly.Models;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Constants = Audibly.App.Helpers.Constants;

namespace Audibly.App.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MiniPlayerPage : Page
{
    private static readonly TimeSpan _skipBackButtonAmount = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan _skipForwardButtonAmount = TimeSpan.FromSeconds(30);
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    public MiniPlayerPage()
    {
        InitializeComponent();

        // Set the title bar for the current view
        App.Window.ExtendsContentIntoTitleBar = true;
        App.Window.SetTitleBar(NowPlayingAppTitleBar);

        DataContext = App.PlayerViewModel;

        // PointerEntered += OnPointerEntered;
        // PointerExited += OnPointerExited;
    }

    /// <summary>
    ///     Gets the app-wide ViewModel instance.
    /// </summary>
    public MainViewModel ViewModel => App.ViewModel;

    /// <summary>
    ///     Gets the app-wide PlayerViewModel instance.
    /// </summary>
    public PlayerViewModel PlayerViewModel => App.PlayerViewModel;

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        OnHoverOverlay.Visibility = Visibility.Collapsed;
        TitleArtistChapterSelectionGrid.Visibility = Visibility.Collapsed;
    }

    private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        OnHoverOverlay.Visibility = Visibility.Visible;
        TitleArtistChapterSelectionGrid.Visibility = Visibility.Visible;
    }

    private void BackButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
            PlayerViewModel.IsPlayerFullScreen = false;
            PlayerViewModel.MaximizeMinimizeGlyph = Constants.MaximizeGlyph;
        }
    }

    private async void ChapterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var container = sender as ComboBox;
        if (container == null || container.SelectedItem is not ChapterInfo chapter) return;

        // get newly selected chapter
        var newChapter = container.SelectedItem as ChapterInfo;

        // check if the newly selected chapter is the same as the current chapter
        if (PlayerViewModel.NowPlaying?.CurrentChapter != null &&
            PlayerViewModel.NowPlaying.CurrentChapter.Equals(newChapter)) return;

        if (newChapter == null) return;

        // check if the newly selected chapter is in a different source file than the current chapter
        if (PlayerViewModel.NowPlaying != null &&
            PlayerViewModel.NowPlaying.CurrentSourceFile.Index != newChapter.ParentSourceFileIndex)
        {
            // set the current source file index to the new source file index
            PlayerViewModel.OpenSourceFile(newChapter.ParentSourceFileIndex, newChapter.Index);
            PlayerViewModel.CurrentPosition = TimeSpan.FromMilliseconds(newChapter.StartTime);
        }
        else if (ChapterCombo.SelectedIndex != ChapterCombo.Items.IndexOf(PlayerViewModel.NowPlaying?.CurrentChapter))
        {
            PlayerViewModel.CurrentPosition = TimeSpan.FromMilliseconds(newChapter.StartTime);
        }

        await PlayerViewModel.NowPlaying.SaveAsync();
    }

    private void PlayPauseButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (PlayerViewModel.PlayPauseIcon == Symbol.Play)
            PlayerViewModel.MediaPlayer.Play();
        else
            PlayerViewModel.MediaPlayer.Pause();
    }

    private async void PreviousChapterButton_Click(object sender, RoutedEventArgs e)
    {
        if (PlayerViewModel.NowPlaying is not null &&
            PlayerViewModel.NowPlaying?.CurrentChapterIndex - 1 > 0 &&
            PlayerViewModel.NowPlaying?.Chapters[(int)(PlayerViewModel.NowPlaying?.CurrentChapterIndex - 1)]
                .ParentSourceFileIndex != PlayerViewModel.NowPlaying?.CurrentSourceFileIndex)
        {
            var newChapterIdx = (int)PlayerViewModel.NowPlaying.CurrentChapterIndex - 1;
            PlayerViewModel.OpenSourceFile(PlayerViewModel.NowPlaying.CurrentSourceFileIndex - 1, newChapterIdx);
            PlayerViewModel.CurrentPosition =
                TimeSpan.FromMilliseconds(PlayerViewModel.NowPlaying.Chapters[newChapterIdx].StartTime);
            await PlayerViewModel.NowPlaying.SaveAsync();

            return;
        }

        var newChapterIndex = PlayerViewModel.NowPlaying.CurrentChapterIndex - 1 >= 0
            ? PlayerViewModel.NowPlaying.CurrentChapterIndex - 1
            : PlayerViewModel.NowPlaying.CurrentChapterIndex;

        if (newChapterIndex == null) return;

        // PlayerViewModel.NowPlaying.CurrentChapter = PlayerViewModel.NowPlaying.Chapters[(int)newChapterIndex];
        PlayerViewModel.NowPlaying.CurrentChapterIndex = newChapterIndex;
        PlayerViewModel.ChapterComboSelectedIndex = (int)newChapterIndex;
        PlayerViewModel.ChapterDurationMs =
            (int)(PlayerViewModel.NowPlaying.CurrentChapter.EndTime -
                  PlayerViewModel.NowPlaying.CurrentChapter.StartTime);
        PlayerViewModel.CurrentPosition =
            TimeSpan.FromMilliseconds(PlayerViewModel.NowPlaying.CurrentChapter.StartTime);

        await PlayerViewModel.NowPlaying.SaveAsync();
    }

    private async void NextChapterButton_Click(object sender, RoutedEventArgs e)
    {
        if (PlayerViewModel.NowPlaying is not null &&
            PlayerViewModel.NowPlaying?.CurrentChapterIndex + 1 < PlayerViewModel.NowPlaying?.Chapters.Count &&
            PlayerViewModel.NowPlaying?.Chapters[(int)(PlayerViewModel.NowPlaying?.CurrentChapterIndex + 1)]
                .ParentSourceFileIndex != PlayerViewModel.NowPlaying?.CurrentSourceFileIndex)
        {
            var newChapterIdx = (int)PlayerViewModel.NowPlaying.CurrentChapterIndex + 1;
            PlayerViewModel.OpenSourceFile(PlayerViewModel.NowPlaying.CurrentSourceFileIndex + 1, newChapterIdx);
            PlayerViewModel.CurrentPosition =
                TimeSpan.FromMilliseconds(PlayerViewModel.NowPlaying.Chapters[newChapterIdx].StartTime);
            await PlayerViewModel.NowPlaying.SaveAsync();

            return;
        }

        var newChapterIndex =
            PlayerViewModel.NowPlaying.CurrentChapterIndex + 1 < PlayerViewModel.NowPlaying.Chapters.Count
                ? PlayerViewModel.NowPlaying.CurrentChapterIndex + 1
                : PlayerViewModel.NowPlaying.CurrentChapterIndex;

        if (newChapterIndex == null) return;

        // PlayerViewModel.NowPlaying.CurrentChapter = PlayerViewModel.NowPlaying.Chapters[(int)newChapterIndex];
        PlayerViewModel.NowPlaying.CurrentChapterIndex = newChapterIndex;
        PlayerViewModel.ChapterComboSelectedIndex = (int)newChapterIndex;
        PlayerViewModel.ChapterDurationMs =
            (int)(PlayerViewModel.NowPlaying.CurrentChapter.EndTime -
                  PlayerViewModel.NowPlaying.CurrentChapter.StartTime);
        PlayerViewModel.CurrentPosition =
            TimeSpan.FromMilliseconds(PlayerViewModel.NowPlaying.CurrentChapter.StartTime);

        await PlayerViewModel.NowPlaying.SaveAsync();
    }

    private async void SkipBackButton_OnClick(object sender, RoutedEventArgs e)
    {
        PlayerViewModel.CurrentPosition = PlayerViewModel.CurrentPosition - _skipBackButtonAmount > TimeSpan.Zero
            ? PlayerViewModel.CurrentPosition - _skipBackButtonAmount
            : TimeSpan.Zero;

        await PlayerViewModel.NowPlaying.SaveAsync();
    }

    private async void SkipForwardButton_OnClick(object sender, RoutedEventArgs e)
    {
        // todo: might need to switch this to using the duration from the audiobook record
        PlayerViewModel.CurrentPosition = PlayerViewModel.CurrentPosition + _skipForwardButtonAmount <=
                                          PlayerViewModel.MediaPlayer.PlaybackSession.NaturalDuration
            ? PlayerViewModel.CurrentPosition + _skipForwardButtonAmount
            : PlayerViewModel.MediaPlayer.PlaybackSession.NaturalDuration;

        await PlayerViewModel.NowPlaying.SaveAsync();
    }
}