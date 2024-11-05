// Author: rstewa · https://github.com/rstewa
// Created: 08/14/2024
// Updated: 09/06/2024

using System;
using System.Linq;
using System.Text;
using Audibly.App.ViewModels;
using Audibly.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.Contacts;
using Windows.ApplicationModel.DataTransfer;

namespace Audibly.App.Views.ControlPages;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SelectFilesDialog : Page
{
    /// <summary>
    ///     Gets the app-wide ViewModel instance.
    /// </summary>
    public MainViewModel ViewModel => App.ViewModel;

    public SelectFilesDialog()
    {
        InitializeComponent();
    }

    //private void SelectedFiles_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    //{
    //    // Prepare a string with one dragged item per line
    //    StringBuilder items = new StringBuilder();
    //    foreach (SelectedFile item in e.Items)
    //    {
    //        if (items.Length > 0) { items.AppendLine(); }
    //        if (item.ToString() != null)
    //        {
    //            // Append name from contact object onto data string
    //            items.Append(item.ToString() + " " + item.FileName);
    //        }
    //    }
    //    // Set the content of the DataPackage
    //    e.Data.SetText(items.ToString());

    //    e.Data.RequestedOperation = DataPackageOperation.Move;
    //}

    private void SelectedFiles_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Move;
    }

    private void SelectedFilesListView_OnDragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs e)
    {
        // update the SelectedFiles observable collection
        var selectedFiles = SelectedFilesListView.Items.Cast<SelectedFile>().ToList();
        ViewModel.SelectedFiles.Clear();
        foreach (var selectedFile in selectedFiles) ViewModel.SelectedFiles.Add(selectedFile);
    }

    private void DeleteFileButton_OnClick(object sender, RoutedEventArgs e)
    {
        var selectedFile = (SelectedFile)((Button)sender).DataContext;
        ViewModel.SelectedFiles.Remove(selectedFile);
    }
}