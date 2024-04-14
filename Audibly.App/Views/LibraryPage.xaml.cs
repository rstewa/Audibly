// Author: rstewa · https://github.com/rstewa
// Created: 3/21/2024
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
using Microsoft.UI.Xaml.Navigation;
using Sharpener.Extensions;

namespace Audibly.App.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class LibraryPage : Page
{
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    /// <summary>
    ///     Initializes the page.
    /// </summary>
    public LibraryPage()
    {
        InitializeComponent();

#if DEBUG
        DeleteButton.Visibility = Visibility.Visible;
#endif
    }

    /// <summary>
    ///     Gets the app-wide ViewModel instance.
    /// </summary>
    public MainViewModel ViewModel => App.ViewModel;

    /// <summary>
    ///     Gets the app-wide PlayerViewModel instance.
    /// </summary>
    public PlayerViewModel PlayerViewModel => App.PlayerViewModel;

    /// <summary>
    ///     Initializes the AutoSuggestBox portion of the search box.
    /// </summary>
    private void AudiobookSearchBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (AudiobookSearchBox == null) return;
        AudiobookSearchBox.AutoSuggestBox.QuerySubmitted += AudiobookSearchBox_QuerySubmitted;
        AudiobookSearchBox.AutoSuggestBox.TextChanged += AudiobookSearchBox_TextChanged;
        AudiobookSearchBox.AutoSuggestBox.PlaceholderText = "Search audiobooks...";
    }

    /// <summary>
    ///     Updates the search box items source when the user changes the search text.
    /// </summary>
    private async void AudiobookSearchBox_TextChanged(AutoSuggestBox sender,
        AutoSuggestBoxTextChangedEventArgs args)
    {
        // We only want to get results when it was a user typing,
        // otherwise we assume the value got filled in by TextMemberPath
        // or the handler for SuggestionChosen.
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            // If no search query is entered, refresh the complete list.
            if (string.IsNullOrEmpty(sender.Text))
            {
                await _dispatcherQueue.EnqueueAsync(async () =>
                    await ViewModel.GetAudiobookListAsync());
                sender.ItemsSource = null;
            }
            else
            {
                // sender.ItemsSource = GetFilteredAudiobooks(sender.Text);
                sender.ItemsSource = GetAudiobookTitles(sender.Text);
            }
        }
    }

    private List<string> GetAudiobookTitles(string text)
    {
        var parameters = text.Split(new[] { ' ' },
            StringSplitOptions.RemoveEmptyEntries);

        return ViewModel.Audiobooks.Where(audiobook => parameters
                .Any(parameter =>
                    audiobook.Title.Contains(parameter, StringComparison.OrdinalIgnoreCase)))
            .Select(audiobook => audiobook.Title)
            .AsList();
    }

    /// <summary>
    ///     Filters or resets the audiobook list based on the search text.
    /// </summary>
    private async void AudiobookSearchBox_QuerySubmitted(AutoSuggestBox sender,
        AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (string.IsNullOrEmpty(args.QueryText))
            await ResetAudiobookList();
        else
            await FilterAudiobookList(args.QueryText);
    }

    /// <summary>
    ///     Resets the audiobook list.
    /// </summary>
    private async Task ResetAudiobookList()
    {
        await _dispatcherQueue.EnqueueAsync(async () =>
            await ViewModel.GetAudiobookListAsync());
    }

    private List<AudiobookViewModel> GetFilteredAudiobooks(string text)
    {
        var parameters = text.Split(new[] { ' ' },
            StringSplitOptions.RemoveEmptyEntries);

        return ViewModel.Audiobooks.Where(audiobook => parameters
                .Any(parameter =>
                    audiobook.Author.Contains(parameter, StringComparison.OrdinalIgnoreCase) ||
                    audiobook.Title.Contains(parameter, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(audiobook => parameters.Count(parameter =>
                audiobook.Author.Contains(parameter, StringComparison.OrdinalIgnoreCase) ||
                audiobook.Title.Contains(parameter, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    /// <summary>
    ///     Filters the audiobook list based on the search text.
    /// </summary>
    private async Task FilterAudiobookList(string text)
    {
        var matches = GetFilteredAudiobooks(text);

        await _dispatcherQueue.EnqueueAsync(() =>
        {
            ViewModel.Audiobooks.Clear();
            foreach (var match in matches) ViewModel.Audiobooks.Add(match);
        });
    }

    /// <summary>
    ///     Resets the audiobook list when leaving the page.
    /// </summary>
    protected override async void OnNavigatedFrom(NavigationEventArgs e)
    {
        await ResetAudiobookList();
    }

    /// <summary>
    ///     Applies any existing filter when navigating to the page.
    /// </summary>
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(AudiobookSearchBox.AutoSuggestBox.Text))
            await FilterAudiobookList(AudiobookSearchBox.AutoSuggestBox.Text);
    }

    private void AudiobookListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedAudiobook = (sender as ListView)?.SelectedItem as AudiobookViewModel;
        ViewModel.SelectedAudiobook = selectedAudiobook;
    }

    private async void PlayThisBookButton_OnClick(object sender, RoutedEventArgs e)
    {
        await _dispatcherQueue.EnqueueAsync(() =>
        {
            if (PlayerViewModel.NowPlaying != null)
                PlayerViewModel.NowPlaying.IsNowPlaying = false;

            PlayerViewModel.NowPlaying = ViewModel.SelectedAudiobook;
            PlayerViewModel.NowPlaying.IsNowPlaying = true;
            PlayerViewModel.MediaPlayer.Source = MediaSource.CreateFromUri(PlayerViewModel.NowPlaying.FilePath.AsUri());
        });
    }

    // TODO: find out if there is a better way to do this
    private async void LibraryPage_OnLoaded(object sender, RoutedEventArgs e)
    {
        // await ViewModel.GetAudiobookListAsync();
        // await _dispatcherQueue.EnqueueAsync(() =>
        // {
        //     PlayerViewModel.NowPlaying = ViewModel.Audiobooks.FirstOrDefault(x => x.IsNowPlaying);
        // });
    }
}