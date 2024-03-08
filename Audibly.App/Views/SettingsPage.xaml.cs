// Author: rstewa
// Created: 3/5/2024
// Updated: 3/7/2024

using System;
using Windows.Storage;
using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.Views;

public sealed partial class SettingsPage : Page
{
    public const string DataSourceKey = "data_source";

    /// <summary>
    ///     Initializes a new instance of the SettingsPage class.
    /// </summary>
    public SettingsPage()
    {
        InitializeComponent();
    }

    /// <summary>
    ///     Launches the privacy statement in the user's default browser.
    /// </summary>
    private async void OnPrivacyButtonClick(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("https://go.microsoft.com/fwlink/?LinkId=521839"));
    }

    /// <summary>
    ///     Launches the license terms in the user's default browser.
    /// </summary>
    private async void OnLicenseButtonClick(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("https://go.microsoft.com/fwlink/?LinkId=822631"));
    }

    /// <summary>
    ///     Launches the sample's GitHub page in the user's default browser.
    /// </summary>
    private async void OnCodeButtonClick(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(
            new Uri("https://github.com/Microsoft/Windows-appsample-customers-orders-database"));
    }

    // TODO: tell user they need to restart the app for changes to take effect
    private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        // Save theme choice to LocalSettings. 
        // ApplicationTheme enum values: 0 = Light, 1 = Dark
        ApplicationData.Current.LocalSettings.Values["themeSetting"] =
            ((ToggleSwitch)sender).IsOn ? 0 : 1;
    }

    private void ToggleSwitch_Loaded(object sender, RoutedEventArgs e)
    {
        ((ToggleSwitch)sender).IsOn = Application.Current.RequestedTheme == ApplicationTheme.Light;
    }
}