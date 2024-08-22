// Author: rstewa · https://github.com/rstewa
// Created: 08/14/2024
// Updated: 08/21/2024

using System;
using System.Collections.Generic;
using System.Linq;
using Audibly.App.ViewModels;
using Audibly.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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

    public List<SelectedFile> SelectedFiles { get; set; }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void SelectedFilesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void SelectedFilesListView_OnDragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        // update the SelectedFiles observable collection
        var selectedFiles = SelectedFilesListView.Items.Cast<SelectedFile>().ToList();
        ViewModel.SelectedFiles.Clear();
        foreach (var selectedFile in selectedFiles)
        {
            ViewModel.SelectedFiles.Add(selectedFile);
        }
    }
}