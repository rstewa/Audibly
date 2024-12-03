// Author: rstewa · https://github.com/rstewa
// Created: 3/29/2024
// Updated: 4/9/2024

using System;
using System.Reflection;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Audibly.App.Helpers;
using Audibly.App.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.Views;

public sealed partial class SettingsPage : Page
{
    /// <summary>
    ///     Gets the app-wide ViewModel instance.
    /// </summary>
    public MainViewModel ViewModel => App.ViewModel;

    public static string Version => Constants.Version;

    public SettingsPage()
    {
        InitializeComponent();
        Loaded += OnSettingsPageLoaded;
    }

    private void OnSettingsPageLoaded(object sender, RoutedEventArgs e)
    {
        var currentTheme = ThemeHelper.RootTheme;
        switch (currentTheme)
        {
            case ElementTheme.Light:
                themeMode.SelectedIndex = 0;
                break;
            case ElementTheme.Dark:
                themeMode.SelectedIndex = 1;
                break;
            case ElementTheme.Default:
                themeMode.SelectedIndex = 2;
                break;
        }
    }

    private void themeMode_SelectionChanged(object sender, RoutedEventArgs e)
    {
        var selectedTheme = ((ComboBoxItem)themeMode.SelectedItem)?.Tag?.ToString();
        var window = WindowHelper.GetWindowForElement(this);
        if (selectedTheme != null)
        {
            ThemeHelper.RootTheme = App.GetEnum<ElementTheme>(selectedTheme);
            if (selectedTheme == "Dark")
                TitleBarHelper.SetCaptionButtonColors(window, Colors.White);
            else if (selectedTheme == "Light")
            {
                // warn user that the light theme is in beta and may not be fully supported
                App.ViewModel.MessageService.ShowDialog(DialogType.Info, "Light Theme Warning",
                    "The light theme is in beta and may not be fully supported. Please report any issues you encounter.");
                
                TitleBarHelper.SetCaptionButtonColors(window, Colors.Black);
            }
            else
                _ = TitleBarHelper.ApplySystemThemeToCaptionButtons(window) == Colors.White ? "Dark" : "Light";
        }
    }

    private async void bugRequestCard_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("https://github.com/rstewa/Audibly/issues/new/choose"));
    }
    
    private async void donateCard_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("https://paypal.me/rstewa35"));
    }

    private async void libationCard_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("https://github.com/rmcrackan/Libation"));
    }

    private async void openAudibleCard_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("https://openaudible.org/"));
    }

    private void contactCard_Click(object sender, RoutedEventArgs e)
    {
        // copy email to clipboard
        const string email = "help@audibly.info";
        var dataPackage = new DataPackage();
        dataPackage.SetText(email);
        Clipboard.SetContent(dataPackage);

        // change icon to checkmark
        DispatcherQueue.TryEnqueue(() => CopyIcon.Glyph = "\uE8FB");
        
        // wait 1 second and change back to copy icon
        Task.Delay(1000).ContinueWith(_ => DispatcherQueue.TryEnqueue(() => CopyIcon.Glyph = "\uE8C8"));
    }

    private void advancedSettingsToggle_Toggled(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }
}