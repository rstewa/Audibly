// Author: rstewa
// Created: 3/5/2024
// Updated: 3/6/2024

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Audibly.App.ViewModels;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

namespace Audibly.App.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AudiobookListPage : Page
{
    private readonly DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    /// <summary>
    ///     Initializes the page.
    /// </summary>
    public AudiobookListPage()
    {
        InitializeComponent();
    }

    /// <summary>
    ///     Gets the app-wide ViewModel instance.
    /// </summary>
    public MainViewModel ViewModel => App.ViewModel;

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
                await dispatcherQueue.EnqueueAsync(async () =>
                    await ViewModel.GetAudiobookListAsync());
                sender.ItemsSource = null;
            }
            else
            {
                sender.ItemsSource = GetFilteredAudiobooks(sender.Text);
            }
        }
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
        await dispatcherQueue.EnqueueAsync(async () =>
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

        await dispatcherQueue.EnqueueAsync(() =>
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

    /// <summary>
    ///     Menu flyout click control for selecting a audiobook and displaying details.
    /// </summary>
    private void ViewDetails_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedAudiobook != null)
            Frame.Navigate(typeof(AudiobookDetailPage), ViewModel.SelectedAudiobook.Model.Id,
                new DrillInNavigationTransitionInfo());
    }

    // private void DataGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) =>
    //     Frame.Navigate(typeof(AudiobookDetailPage), ViewModel.SelectedAudiobook.Model.Id,
    //             new DrillInNavigationTransitionInfo());

    /// <summary>
    ///     Navigates to a blank audiobook details page for the user to fill in.
    /// </summary>
    private void CreateAudiobook_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(AudiobookDetailPage), null, new DrillInNavigationTransitionInfo());
    }

    /// <summary>
    ///     Selects the tapped audiobook.
    /// </summary>
    private void DataGrid_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        ViewModel.SelectedAudiobook = (e.OriginalSource as FrameworkElement).DataContext as AudiobookViewModel;
    }
}