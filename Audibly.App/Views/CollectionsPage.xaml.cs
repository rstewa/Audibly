// Author: rstewa · https://github.com/rstewa
// Updated: 03/17/2025

using Audibly.App.Services;
using Audibly.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.Views;

/// <summary>
///     A page that displays a list of folders.
/// </summary>
public sealed partial class CollectionsPage : Page
{
    public CollectionsPage()
    {
        InitializeComponent();
    }

    /// <summary>
    ///     Gets the app-wide ViewModel instance.
    /// </summary>
    public MainViewModel ViewModel => App.ViewModel;

    private async void AddCollection_Click(object sender, RoutedEventArgs e)
    {
        // show dialog
        await DialogService.ShowCreateCollectionDialogAsync();
        await ViewModel.GetFileSystemItemsAsync();
    }

    private void Folder_Click(object sender, ItemClickEventArgs e)
    {
        ;
    }
}