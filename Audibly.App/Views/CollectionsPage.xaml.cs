// Author: rstewa · https://github.com/rstewa
// Updated: 03/17/2025

using Audibly.App.Services;
using Audibly.App.ViewModels;
using Audibly.App.ViewModels.Interfaces;
using Audibly.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

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
        // get the clicked folder
        var folder = e.ClickedItem as FileSystemItem;
    }

    private async void RefreshButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.GetFileSystemItemsAsync();
    }

    private void DeleteCollection_Click(object sender, RoutedEventArgs e)
    {
        
    }

    private void CollectionsListView_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        // Get the tapped item
        if (sender is ListView { SelectedItem: IFileSystemItem selectedItem })
        {
            Frame.Navigate(typeof(CollectionContentPage), (selectedItem as CollectionViewModel));
        }
    }
}