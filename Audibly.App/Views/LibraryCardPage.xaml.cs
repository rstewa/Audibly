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

    // private void AudiobookSearchBox_Loaded(object sender, RoutedEventArgs e)
    // {
    //     // if (AudiobookSearchBox == null) return;
    //     // AudiobookSearchBox.AutoSuggestBox.QuerySubmitted += AudiobookSearchBox_QuerySubmitted;
    //     // AudiobookSearchBox.AutoSuggestBox.TextChanged += AudiobookSearchBox_TextChanged;
    //     // AudiobookSearchBox.AutoSuggestBox.PlaceholderText = "Search audiobooks...";
    // }
    //
    // /// <summary>
    // ///     Filters or resets the audiobook list based on the search text.
    // /// </summary>
    // private async void AudiobookSearchBox_QuerySubmitted(AutoSuggestBox sender,
    //     AutoSuggestBoxQuerySubmittedEventArgs args)
    // {
    //     if (string.IsNullOrEmpty(args.QueryText))
    //         await ViewModel.ResetAudiobookList();
    //     else
    //         await FilterAudiobookList(args.QueryText);
    // }

    /// <summary>
    ///     Resets the audiobook list.
    /// </summary>
    // private async Task ResetAudiobookList()
    // {
    //     await _dispatcherQueue.EnqueueAsync(async () =>
    //         await ViewModel.GetAudiobookListAsync());
    // }

    // private List<AudiobookViewModel> GetFilteredAudiobooks(string text)
    // {
    //     var parameters = text.Split(new[] { ' ' },
    //         StringSplitOptions.RemoveEmptyEntries);
    //
    //     return ViewModel.Audiobooks.Where(audiobook => parameters
    //             .Any(parameter =>
    //                 audiobook.Author.Contains(parameter, StringComparison.OrdinalIgnoreCase) ||
    //                 audiobook.Title.Contains(parameter, StringComparison.OrdinalIgnoreCase)))
    //         .OrderByDescending(audiobook => parameters.Count(parameter =>
    //             audiobook.Author.Contains(parameter, StringComparison.OrdinalIgnoreCase) ||
    //             audiobook.Title.Contains(parameter, StringComparison.OrdinalIgnoreCase)))
    //         .ToList();
    // }
    //
    // /// <summary>
    // ///     Filters the audiobook list based on the search text.
    // /// </summary>
    // private async Task FilterAudiobookList(string text)
    // {
    //     var matches = GetFilteredAudiobooks(text);
    //
    //     await _dispatcherQueue.EnqueueAsync(() =>
    //     {
    //         ViewModel.Audiobooks.Clear();
    //         foreach (var match in matches) ViewModel.Audiobooks.Add(match);
    //     });
    // }
    //
    // /// <summary>
    // ///     Updates the search box items source when the user changes the search text.
    // /// </summary>
    // private async void AudiobookSearchBox_TextChanged(AutoSuggestBox sender,
    //     AutoSuggestBoxTextChangedEventArgs args)
    // {
    //     // We only want to get results when it was a user typing,
    //     // otherwise we assume the value got filled in by TextMemberPath
    //     // or the handler for SuggestionChosen.
    //     if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
    //     {
    //         // If no search query is entered, refresh the complete list.
    //         if (string.IsNullOrEmpty(sender.Text))
    //         {
    //             await _dispatcherQueue.EnqueueAsync(async () =>
    //                 await ViewModel.GetAudiobookListAsync());
    //             sender.ItemsSource = null;
    //         }
    //         else
    //         {
    //             // sender.ItemsSource = GetFilteredAudiobooks(sender.Text);
    //             sender.ItemsSource = GetAudiobookTitles(sender.Text);
    //         }
    //     }
    // }
    //
    // private List<string> GetAudiobookTitles(string text)
    // {
    //     var parameters = text.Split(new[] { ' ' },
    //         StringSplitOptions.RemoveEmptyEntries);
    //
    //     return ViewModel.Audiobooks.Where(audiobook => parameters
    //             .Any(parameter =>
    //                 audiobook.Title.Contains(parameter, StringComparison.OrdinalIgnoreCase)))
    //         .Select(audiobook => audiobook.Title)
    //         .AsList();
    // }
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