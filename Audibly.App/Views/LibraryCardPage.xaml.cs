// Author: rstewa · https://github.com/rstewa
// Created: 3/29/2024
// Updated: 4/9/2024

using Audibly.App.Helpers;
using Audibly.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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

#if DEBUG
        DeleteButton.Visibility = Visibility.Visible;
        TestButton.Visibility = Visibility.Visible;
#endif
    }

    private void TestButton_OnClick(object sender, RoutedEventArgs e)
    {
        var text = Changelog.Text;
        ViewModel.MessageService.ShowDialog(DialogType.Changelog, $"Welcome to Audibly v{App.Version}", text);
    }
}