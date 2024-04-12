// Author: rstewa · https://github.com/rstewa
// Created: 3/29/2024
// Updated: 4/8/2024

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;
using Audibly.App.Helpers;
using Audibly.App.ViewModels;
using Audibly.App.Views;
using Audibly.App.Views.ControlPages;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace Audibly.App;

/// <summary>
///     The "chrome" layer of the app that provides top-level navigation with
///     proper keyboarding navigation.
/// </summary>
public sealed partial class AppShell : Page
{
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    /// <summary>
    ///     Gets the app-wide ViewModel instance.
    /// </summary>
    public MainViewModel ViewModel => App.ViewModel;

    /// <summary>
    ///     Initializes a new instance of the AppShell, sets the static 'Current' reference,
    ///     adds callbacks for Back requests and changes in the SplitView's DisplayMode, and
    ///     provide the nav menu list with the data to display.
    /// </summary>
    public AppShell()
    {
        InitializeComponent();

        // Loaded += (sender, args) => { NavView.SelectedItem = AudiobookListMenuItem; };
        // Loaded += (_, _) =>
        // {
        //     NavView.SelectedItem = LibraryMenuItem;
        //     var window = App.Window; // idk if this works or not
        //     window.Title = AppTitleText;
        //     window.ExtendsContentIntoTitleBar = true;
        //     window.SetTitleBar(AppTitleBar);
        // };
        
        ViewModel.MessageService.ShowDialogRequested += OnShowDialogRequested;
    }

    private void AppShell_OnLoaded(object sender, RoutedEventArgs e)
    {
        // NOTE: for debugging
        // ApplicationData.Current.LocalSettings.Values.Remove("HasCompletedOnboarding");
        
        // Check to see if this is the first time the app is being launched
        var hasCompletedOnboarding = ApplicationData.Current.LocalSettings.Values.FirstOrDefault(x => x.Key == "HasCompletedOnboarding");
        if (hasCompletedOnboarding.Value == null)
        {
            ApplicationData.Current.LocalSettings.Values["HasCompletedOnboarding"] = true;
            ViewModel.MessageService.ShowDialog(DialogType.Info, "Welcome to Audibly!", "We're glad you're here. Let's get started by adding your first audiobook.");
        }
    }

    private async void ShowDeleteDialogAsync(string title, string content)
    {
        await _dispatcherQueue.EnqueueAsync(async () =>
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = "Remove from Library",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };

            dialog.PrimaryButtonClick += async (_, _) =>
            {
                await ViewModel.DeleteAudiobookAsync();
                await ViewModel.GetAudiobookListAsync();
            };

            await dialog.ShowAsync();
        });
    }

    private async void ShowOkDialogAsync(string title, string content)
    {
        await _dispatcherQueue.EnqueueAsync(async () =>
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "Ok",
                XamlRoot = XamlRoot,
                DefaultButton = ContentDialogButton.Close
            };

            await dialog.ShowAsync();
        });
    }
    
    private async void ShowRestartDialogAsync(string title, string content)
    {
        await _dispatcherQueue.EnqueueAsync(async () =>
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = "Restart",
                DefaultButton = ContentDialogButton.Primary,
                CloseButtonText = "Not Now",
                XamlRoot = XamlRoot
            };
            
            dialog.PrimaryButtonClick += (_, _) =>
            {
                App.RestartApp();
            };

            await dialog.ShowAsync();
        });
    }

    private async void ShowChangelogDialog(string title, string changelogText)
    {
        var dialogContent = new ChangelogDialogContent(title, changelogText);
        var contentDialog = new ContentDialog
        {
            Content = dialogContent,
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };
        await contentDialog.ShowAsync();
    }
    
    private void OnShowDialogRequested(DialogType type, string title, string content)
    {
        switch (type)
        {
            case DialogType.Error:
                ShowDeleteDialogAsync(title, content);
                break;
            case DialogType.Info:
                ShowOkDialogAsync(title, content);
                break;
            case DialogType.Restart:
                ShowRestartDialogAsync(title, content);
                break; 
            case DialogType.Changelog:
                ShowChangelogDialog(title, content);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public string AppTitleText => "Audibly";

    /// <summary>
    ///     Gets the navigation frame instance.
    /// </summary>
    public Frame AppFrame => frame;

    public readonly string AudiobookListLabel = "Audiobooks";
    public readonly string LibraryLabel = "Library";
    public readonly string NowPlayingLabel = "Now Playing";

    /// <summary>
    ///     Navigates to the page corresponding to the tapped item.
    /// </summary>
    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is not NavigationViewItem item) return;

        // if (item == AudiobookListMenuItem)
        // {
        //     AppFrame.Navigate(typeof(LibraryPage));
        // }
        if (item == LibraryMenuItem)
            AppFrame.Navigate(typeof(LibraryCardPage));
        // else if (item == NowPlayingMenuItem)
        //     AppFrame.Navigate(typeof(PlayerPage));
        else if (item == NavView.SettingsItem) AppFrame.Navigate(typeof(SettingsPage));
    }

    /// <summary>
    ///     Ensures the nav menu reflects reality when navigation is triggered outside of
    ///     the nav menu buttons.
    /// </summary>
    private void OnNavigatingToPage(object sender, NavigatingCancelEventArgs e)
    {
        if (e.NavigationMode == NavigationMode.Back)
        {
            // if (e.SourcePageType == typeof(LibraryPage)) NavView.SelectedItem = AudiobookListMenuItem;
            if (e.SourcePageType == typeof(LibraryCardPage)) NavView.SelectedItem = LibraryMenuItem;
            // else if (e.SourcePageType == typeof(PlayerPage)) NavView.SelectedItem = NowPlayingMenuItem;
            else if (e.SourcePageType == typeof(SettingsPage)) NavView.SelectedItem = NavView.SettingsItem;
        }
    }

    /// <summary>
    ///     Invoked when the View Code button is clicked. Launches the repo on GitHub.
    /// </summary>
    private async void ViewCodeNavPaneButton_Tapped(object sender, TappedRoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("https://github.com/rstewa/audibly"));
    }

    /// <summary>
    ///     Navigates the frame to the previous page.
    /// </summary>
    private void NavigationView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        if (AppFrame.CanGoBack) AppFrame.GoBack();
    }

    private void AudiobookSearchBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (AudiobookSearchBox == null) return;
        AudiobookSearchBox.QuerySubmitted += AudiobookSearchBox_QuerySubmitted;
        AudiobookSearchBox.TextChanged += AudiobookSearchBox_TextChanged;
        AudiobookSearchBox.PlaceholderText = "Search audiobooks...";
    }

    // TODO: there's a bug when backspacing search text, it doesn't reset the list

    /// <summary>
    ///     Filters or resets the audiobook list based on the search text.
    /// </summary>
    private async void AudiobookSearchBox_QuerySubmitted(AutoSuggestBox sender,
        AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (string.IsNullOrEmpty(args.QueryText))
            await ViewModel.ResetAudiobookList();
        else
            await FilterAudiobookList(args.QueryText);
    }

    private List<AudiobookViewModel> GetFilteredAudiobooks(string text)
    {
        var parameters = text.Split(new[] { ' ' },
            StringSplitOptions.RemoveEmptyEntries);

        var matches = ViewModel.Audiobooks
            .Select(audiobook => new
            {
                Audiobook = audiobook,
                Score = parameters.Count(parameter =>
                    audiobook.Author.Contains(parameter, StringComparison.OrdinalIgnoreCase) ||
                    audiobook.Title.Contains(parameter, StringComparison.OrdinalIgnoreCase))
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Audiobook)
            .ToList();

        var exactMatches = matches.Where(audiobook =>
            audiobook.Author.Equals(text, StringComparison.OrdinalIgnoreCase) ||
            audiobook.Title.Equals(text, StringComparison.OrdinalIgnoreCase)).ToList();
        return exactMatches.Any() ? exactMatches : matches;
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
                sender.ItemsSource = GetAudiobookTitles(sender.Text).Concat(GetAudiobookAuthors(sender.Text));
                await FilterAudiobookList(sender.Text);
            }
        }
    }

    private List<string> GetAudiobookTitles(string text)
    {
        var parameters = text.Split(new[] { ' ' },
            StringSplitOptions.RemoveEmptyEntries);

        return ViewModel.Audiobooks
            .Select(audiobook => new
            {
                audiobook.Title,
                Score = parameters.Count(parameter =>
                    audiobook.Title.Contains(parameter, StringComparison.OrdinalIgnoreCase))
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Title)
            .ToList();
    }

    private List<string> GetAudiobookAuthors(string text)
    {
        var parameters = text.Split(new[] { ' ' },
            StringSplitOptions.RemoveEmptyEntries);

        return ViewModel.Audiobooks
            .Select(audiobook => new
            {
                audiobook.Author,
                Score = parameters.Count(parameter =>
                    audiobook.Author.Contains(parameter, StringComparison.OrdinalIgnoreCase))
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Author)
            .Distinct()
            .ToList();
    }
}