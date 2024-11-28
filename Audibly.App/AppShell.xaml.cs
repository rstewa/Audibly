// Author: rstewa · https://github.com/rstewa
// Created: 04/15/2024
// Updated: 10/03/2024

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
using Constants = Audibly.App.Helpers.Constants;
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
    ///     Gets the app-wide PlayerViewModel instance.
    /// </summary>
    public PlayerViewModel PlayerViewModel => App.PlayerViewModel;

    /// <summary>
    ///     Initializes a new instance of the AppShell, sets the static 'Current' reference,
    ///     adds callbacks for Back requests and changes in the SplitView's DisplayMode, and
    ///     provide the nav menu list with the data to display.
    /// </summary>
    public AppShell()
    {
        InitializeComponent();

        // set the title bar
        var window = WindowHelper.GetMainWindow();
        if (window != null)
            window.SetTitleBar(AppTitleBar);

        AppShellFrame.Navigate(typeof(LibraryCardPage));

        Loaded += (_, _) => { NavView.SelectedItem = LibraryCardMenuItem; };

        ViewModel.MessageService.ShowDialogRequested += OnShowDialogRequested;
        App.ViewModel.FileImporter.ImportCompleted += HideImportDialog;
        App.ViewModel.ProgressDialogCompleted += HideProgressDialog;

        NavView.PaneClosed += (_, _) => { UserSettings.IsSidebarCollapsed = true; };
        NavView.PaneOpened += (_, _) => { UserSettings.IsSidebarCollapsed = false; };
    }

    private async void AppShell_OnLoaded(object sender, RoutedEventArgs e)
    {
        // Check to see if this is the first time the app is being launched
        var hasCompletedOnboarding =
            ApplicationData.Current.LocalSettings.Values.FirstOrDefault(x => x.Key == "HasCompletedOnboarding");
        if (hasCompletedOnboarding.Value == null)
        {
            ApplicationData.Current.LocalSettings.Values["HasCompletedOnboarding"] = true;

            // // check if user had v1 and was listening to an audiobook
            // var currentAudiobookPath = ApplicationData.Current.LocalSettings.Values["CurrentAudiobookPath"]?.ToString();
            // if (currentAudiobookPath != null)
            // {
            //     var result = await ShowYesNoDialogAsync("Welcome Back!",
            //         "We've detected that you were listening to an audiobook in a previous version of Audibly. Would you like to continue listening?");
            //     if (!result) return;
            //
            //     // get that audiobooks current position
            //     var name = Path.GetFileNameWithoutExtension(currentAudiobookPath);
            //     var currentPosition =
            //         ApplicationData.Current.LocalSettings.Values[$"{name}:CurrentPosition"]?.ToDouble();
            //
            //     // import the audiobook
            //     var importSuccess = await ViewModel.ImportAudiobookTest(currentAudiobookPath);
            //     if (!importSuccess) return;
            //
            //     // set the current position
            //     var audiobook = ViewModel.Audiobooks.FirstOrDefault(a => a. == currentAudiobookPath);
            //     if (audiobook == null) return;
            //
            //     ViewModel.SelectedAudiobook = audiobook;
            //     if (currentPosition != null)
            //         audiobook.CurrentTimeMs = (int)currentPosition;
            //     PlayerViewModel.OpenAudiobook(audiobook);
            // }
            // else
            // {
            ViewModel.MessageService.ShowDialog(DialogType.Info, "Welcome to Audibly!",
                "We're glad you're here. Let's get started by adding your first audiobook.");
            // }
        }

        // check for current version key
        var userCurrentVersion = ApplicationData.Current.LocalSettings.Values["CurrentVersion"]?.ToString();
        if (userCurrentVersion == null || userCurrentVersion != Constants.Version)
        {
            ApplicationData.Current.LocalSettings.Values["CurrentVersion"] = Constants.Version;
            ViewModel.MessageService.ShowDialog(DialogType.Changelog, "What's New?", Changelog.Text);
        }

        await ProcessDialogQueue();
        
        // get sidebar state
        if (UserSettings.IsSidebarCollapsed)
        {
            NavView.IsPaneOpen = false;
            NavView.CompactModeThresholdWidth = 0;
        }
    }

    private ContentDialog? _importDialog;

    private ContentDialog? _progressDialog;

    private async Task<bool> ShowYesNoDialogAsync(string title, string content)
    {
        var result = false;
        await _dispatcherQueue.EnqueueAsync(async () =>
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = "Yes",
                CloseButtonText = "No",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot,
                RequestedTheme = ThemeHelper.ActualTheme
            };

            dialog.PrimaryButtonClick += (_, _) => { result = true; };

            dialog.CloseButtonClick += (_, _) => { result = false; };

            await dialog.ShowAsync();
        });
        return result;
    }

    private ContentDialog CreateImportDialog(string title)
    {
        var importDialog = new ProgressDialogContent();
        _importDialog = new ContentDialog
        {
            Title = title,
            Content = importDialog,
            DefaultButton = ContentDialogButton.Close,
            CloseButtonText = "Cancel",
            RequestedTheme = ThemeHelper.ActualTheme
        };

        _importDialog.CloseButtonClick += (_, _) =>
        {
            ViewModel.MessageService.CancelDialog();
            ViewModel.IsLoading = false;
            ViewModel.Refresh();
        };

        return _importDialog;
    }

    private void HideImportDialog()
    {
        if (_importDialog == null) return;

        _dispatcherQueue.TryEnqueue(() =>
        {
            _importDialog.Hide();
            // _importDialog = null;
        });
    }

    private void HideProgressDialog()
    {
        if (_progressDialog == null) return;

        _dispatcherQueue.TryEnqueue(() =>
        {
            _progressDialog.Hide();
            // _progressDialog = null;
        });
    }

    private ContentDialog CreateDeleteDialog(string title, string content)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = "Remove from Library",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            RequestedTheme = ThemeHelper.ActualTheme
        };

        dialog.PrimaryButtonClick += async (_, _) =>
        {
            await ViewModel.DeleteAudiobookAsync();
            await ViewModel.GetAudiobookListAsync();
        };

        return dialog;
    }

    private ContentDialog CreateOkDialog(string title, string content)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            CloseButtonText = "Ok",
            DefaultButton = ContentDialogButton.Close,
            RequestedTheme = ThemeHelper.ActualTheme
        };

        return dialog;
    }

    private ContentDialog CreateRestartDialog(string title, string content)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = "Restart",
            DefaultButton = ContentDialogButton.Primary,
            CloseButtonText = "Not Now",
            RequestedTheme = ThemeHelper.ActualTheme
        };

        dialog.PrimaryButtonClick += (_, _) => { App.RestartApp(); };

        return dialog;
    }

    private ContentDialog CreateChangelogDialog(string changelogText)
    {
        var dialogContent = new ChangelogDialogContent(changelogText);
        var dialog = new ContentDialog
        {
            Content = dialogContent,
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Close,
            RequestedTheme = ThemeHelper.ActualTheme
        };

        return dialog;
    }

    private ContentDialog CreateProgressDialog(string title)
    {
        var progressDialog = new ProgressDialogContent();
        _progressDialog = new ContentDialog
        {
            Title = title,
            Content = progressDialog,
            RequestedTheme = ThemeHelper.ActualTheme
        };

        return _progressDialog;
    }

    private readonly Queue<ContentDialog> _dialogQueue = new();

    private async void OnShowDialogRequested(DialogType type, string title, string content, Action? onConfirm)
    {
        var dialog = type switch
        {
            DialogType.Error => CreateDeleteDialog(title, content),
            DialogType.Info => CreateOkDialog(title, content),
            DialogType.Restart => CreateRestartDialog(title, content),
            DialogType.Changelog => CreateChangelogDialog(content),
            DialogType.Import => CreateImportDialog(title),
            DialogType.Progress => CreateProgressDialog(title),
            _ => null
        };

        if (dialog == null) return;

        _dialogQueue.Enqueue(dialog);
        await ProcessDialogQueue();
    }

    private readonly SemaphoreSlim _dialogQueueLock = new(1, 1);

    private async Task ProcessDialogQueue()
    {
        await _dialogQueueLock.WaitAsync();
        try
        {
            while (XamlRoot != null && _dialogQueue.Count > 0)
            {
                var dialog = _dialogQueue.Dequeue();
                dialog.XamlRoot = XamlRoot;
                await dialog.ShowAsync();
            }
        }
        finally
        {
            _dialogQueueLock.Release();
        }
    }

    /// <summary>
    ///     Gets the navigation frame instance.
    /// </summary>
    public Frame AppAppShellFrame => AppShellFrame;

    public readonly string AudiobookListLabel = "Audiobooks";
    public readonly string LibraryLabel = "Library";
    public readonly string NowPlayingLabel = "Now Playing";

    /// <summary>
    ///     Navigates to the page corresponding to the tapped item.
    /// </summary>
    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is not NavigationViewItem item) return;

        // check if the item is already the current page
        // if (item == (NavigationViewItem)NavView.SelectedItem) return;

        // if (item == AudiobookListMenuItem)
        // {
        //     AppFrame.Navigate(typeof(LibraryPage));
        // }
        if (item == LibraryCardMenuItem)
        {
            if (AppAppShellFrame.Content is LibraryCardPage) return;
            AppAppShellFrame.Navigate(typeof(LibraryCardPage));
        }
        else if (item == NowPlayingMenuItem)
        {
            App.RootFrame?.Navigate(typeof(PlayerPage));
            PlayerViewModel.IsPlayerFullScreen = true;
            PlayerViewModel.MaximizeMinimizeGlyph = Constants.MinimizeGlyph;
        }
        else if (item == (NavigationViewItem)NavView.SettingsItem)
        {
            if (AppAppShellFrame.Content is SettingsPage) return;
            AppAppShellFrame.Navigate(typeof(SettingsPage));
        }
    }

    /// <summary>
    ///     Ensures the nav menu reflects reality when navigation is triggered outside
    ///     the nav menu buttons.
    /// </summary>
    private void OnNavigatingToPage(object sender, NavigatingCancelEventArgs e)
    {
        if (e.NavigationMode == NavigationMode.Back)
        {
            // if (e.SourcePageType == typeof(LibraryPage)) NavView.SelectedItem = AudiobookListMenuItem;
            if (e.SourcePageType == typeof(LibraryCardPage)) NavView.SelectedItem = LibraryCardMenuItem;
            else if (e.SourcePageType == typeof(PlayerPage)) NavView.SelectedItem = NowPlayingMenuItem;
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
        if (AppAppShellFrame.CanGoBack) AppAppShellFrame.GoBack();
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
            await ViewModel.ResetAudiobookListAsync();
        else
            await FilterAudiobookList(args.QueryText);
    }

    private List<AudiobookViewModel> GetFilteredAudiobooks(string text)
    {
        var parameters = text.Split([' '],
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

        return exactMatches.Count != 0 ? exactMatches : matches;
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