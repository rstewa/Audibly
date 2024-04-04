// Author: rstewa · https://github.com/rstewa
// Created: 3/5/2024
// Updated: 3/22/2024

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;
using Audibly.App.ViewModels;
using Audibly.App.Views;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Sharpener.Extensions;
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
        Loaded += (sender, args) =>
        {
            NavView.SelectedItem = LibraryMenuItem;
            var window = App.Window; // idk if this works or not
            window.Title = AppTitleText;
            window.ExtendsContentIntoTitleBar = true;
            window.SetTitleBar(this.AppTitleBar);
        };

        // Set up custom title bar.
        // App.Window.ExtendsContentIntoTitleBar = true;
    }

    public string AppTitleText => "Audibly";

    /// <summary>
    ///     Gets the navigation frame instance.
    /// </summary>
    public Frame AppFrame => frame;

    /// <summary>
    ///     Default keyboard focus movement for any unhandled keyboarding
    /// </summary>
    private void AppShell_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        var direction = FocusNavigationDirection.None;
        switch (e.Key)
        {
            case VirtualKey.Left:
            case VirtualKey.GamepadDPadLeft:
            case VirtualKey.GamepadLeftThumbstickLeft:
            case VirtualKey.NavigationLeft:
                direction = FocusNavigationDirection.Left;
                break;
            case VirtualKey.Right:
            case VirtualKey.GamepadDPadRight:
            case VirtualKey.GamepadLeftThumbstickRight:
            case VirtualKey.NavigationRight:
                direction = FocusNavigationDirection.Right;
                break;

            case VirtualKey.Up:
            case VirtualKey.GamepadDPadUp:
            case VirtualKey.GamepadLeftThumbstickUp:
            case VirtualKey.NavigationUp:
                direction = FocusNavigationDirection.Up;
                break;

            case VirtualKey.Down:
            case VirtualKey.GamepadDPadDown:
            case VirtualKey.GamepadLeftThumbstickDown:
            case VirtualKey.NavigationDown:
                direction = FocusNavigationDirection.Down;
                break;
        }

        if (direction != FocusNavigationDirection.None &&
            FocusManager.FindNextFocusableElement(direction) is Control control)
        {
            control.Focus(FocusState.Keyboard);
            e.Handled = true;
        }
    }

    public readonly string AudiobookListLabel = "Audiobooks";
    public readonly string LibraryLabel = "Library";

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
        {
            AppFrame.Navigate(typeof(LibraryCardPage));
        }
        else if (item == NavView.SettingsItem)
        {
            AppFrame.Navigate(typeof(SettingsPage));
        }
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
}