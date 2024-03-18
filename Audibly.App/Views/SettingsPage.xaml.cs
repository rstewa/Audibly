// Author: rstewa · https://github.com/rstewa
// Created: 3/5/2024
// Updated: 3/18/2024

using System;
using System.Reflection;
using Windows.Storage;
using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.Views;

public sealed partial class SettingsPage : Page
{
    public string Version
    {
        get
        {
            var version = Assembly.GetEntryAssembly().GetName().Version;
            return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
        }
    }

    public SettingsPage()
    {
        InitializeComponent();
        Loaded += OnSettingsPageLoaded;
    }

    private void OnSettingsPageLoaded(object sender, RoutedEventArgs e)
    {
        var currentTheme = ApplicationData.Current.LocalSettings.Values["themeSetting"];
        themeMode.SelectedIndex = currentTheme switch
        {
            "Light" => 0,
            "Dark" => 1,
            _ => themeMode.SelectedIndex
        };
    }

    private void themeMode_SelectionChanged(object sender, RoutedEventArgs e)
    {
        var selectedTheme = ((ComboBoxItem)themeMode.SelectedItem)?.Tag?.ToString();
        if (selectedTheme != null) ApplicationData.Current.LocalSettings.Values["themeSetting"] = selectedTheme;
    }

    private async void bugRequestCard_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("https://github.com/rstewa/Audibly/issues/new/choose"));
    }
}