// Author: rstewa · https://github.com/rstewa
// Created: 3/29/2024
// Updated: 4/9/2024

using System;
using System.Reflection;
using Windows.Storage;
using Windows.System;
using Audibly.App.Helpers;
using Audibly.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.Views;

public sealed partial class SettingsPage : Page
{
    /// <summary>
    ///     Gets the app-wide ViewModel instance.
    /// </summary>
    public MainViewModel ViewModel => App.ViewModel;

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

    private bool _isStartup = true;

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
        // TODO
        // if (selectedTheme != null && selectedTheme.Equals("Light")) ViewModel.ThemeService.SetTheme(Theme.Light);
        // else if (selectedTheme != null && selectedTheme.Equals("Dark")) ViewModel.ThemeService.SetTheme(Theme.Dark);
        if (_isStartup)
        {
            _isStartup = false;
            return;
        }

        var selectedTheme = ((ComboBoxItem)themeMode.SelectedItem)?.Tag?.ToString();
        if (selectedTheme == null)
            return;

        ApplicationData.Current.LocalSettings.Values["themeSetting"] = selectedTheme;
        
        var currentTheme = Application.Current.RequestedTheme == ApplicationTheme.Light ? "Light" : "Dark";
        if (selectedTheme.Equals(currentTheme)) return;

        // notify user that theme change will take effect after app restart
        ViewModel.MessageService.ShowDialog(DialogType.Restart, "Theme Change",
            "Theme change will take effect after app restart.");
    }

    private async void bugRequestCard_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("https://github.com/rstewa/Audibly/issues/new/choose"));
    }
}