// Author: rstewa · https://github.com/rstewa
// Created: 3/5/2024
// Updated: 3/22/2024

using System;
using Windows.System;
using Audibly.App.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

namespace Audibly.App;

/// <summary>
///     The "chrome" layer of the app that provides top-level navigation with
///     proper keyboarding navigation.
/// </summary>
public sealed partial class AppShell : Page
{
    /// <summary>
    ///     Initializes a new instance of the AppShell, sets the static 'Current' reference,
    ///     adds callbacks for Back requests and changes in the SplitView's DisplayMode, and
    ///     provide the nav menu list with the data to display.
    /// </summary>
    public AppShell()
    {
        InitializeComponent();

        Loaded += (sender, args) => { NavView.SelectedItem = AudiobookListMenuItem; };

        // Set up custom title bar.
        App.Window.ExtendsContentIntoTitleBar = true;
    }

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

    /// <summary>
    ///     Navigates to the page corresponding to the tapped item.
    /// </summary>
    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        var pageType = args.IsSettingsInvoked ? typeof(SettingsPage) : typeof(LibraryPage);
        if (pageType != null && pageType != AppFrame.CurrentSourcePageType) AppFrame.Navigate(pageType);
    }

    /// <summary>
    ///     Ensures the nav menu reflects reality when navigation is triggered outside of
    ///     the nav menu buttons.
    /// </summary>
    private void OnNavigatingToPage(object sender, NavigatingCancelEventArgs e)
    {
        if (e.NavigationMode == NavigationMode.Back)
        {
            if (e.SourcePageType == typeof(LibraryPage))
                NavView.SelectedItem = AudiobookListMenuItem;
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
}