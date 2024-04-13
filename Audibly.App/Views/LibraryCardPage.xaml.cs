// Author: rstewa · https://github.com/rstewa
// Created: 3/29/2024
// Updated: 4/9/2024

using System;
using Audibly.App.Helpers;
using Audibly.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Audibly.App.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class LibraryCardPage : Page
{
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
    }

    private void TestContentDialogButton_OnClick(object sender, RoutedEventArgs e)
    {
        var text = Changelog.Text;
        ViewModel.MessageService.ShowDialog(DialogType.Changelog, $"Welcome to Audibly v{App.Version}", text);
    }

    private void InfoBar_OnClosed(InfoBar sender, InfoBarClosedEventArgs args)
    {
        // get the notification object
        if (sender.DataContext is not Notification notification) return;
        ViewModel.OnNotificationClosed(notification);
    }

    private void TestNotificationButton_OnClick(object sender, RoutedEventArgs e)
    {
        // randomly select InfoBarSeverity
        var random = new Random();
        var severity = random.Next(0, 4);
        
        ViewModel.EnqueueNotification(new Notification
        {
            Message = "This is a test notification",
            Severity = severity switch
            {
                0 => InfoBarSeverity.Informational,
                1 => InfoBarSeverity.Success,
                2 => InfoBarSeverity.Warning,
                3 => InfoBarSeverity.Error,
                _ => InfoBarSeverity.Informational
            }
        });
    }

    private void SecretMenuKeyboardAccelerator_OnInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        SecretMenuSeparator.Visibility = Visibility.Visible;
        DeleteButton.Visibility = Visibility.Visible;
        TestContentDialogButton.Visibility = Visibility.Visible;
        TestNotificationButton.Visibility = Visibility.Visible;
    }
}