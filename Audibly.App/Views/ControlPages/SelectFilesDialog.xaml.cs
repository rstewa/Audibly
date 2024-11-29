// Author: rstewa · https://github.com/rstewa
// Created: 08/14/2024
// Updated: 09/06/2024

using System.Collections.Specialized;
using Audibly.App.Extensions;
using Audibly.App.ViewModels;
using Audibly.Models;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.Views.ControlPages;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SelectFilesDialog : Page
{
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    public SelectFilesDialog()
    {
        InitializeComponent();

        ViewModel.SelectedFiles.CollectionChanged += SelectedFiles_CollectionChanged;
    }

    public MainViewModel ViewModel => App.ViewModel;

    private void SelectedFiles_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var action = e.Action;
        if (action == NotifyCollectionChangedAction.Add)
            // Call ReapplyAlternateColors after the collection changes
            _dispatcherQueue.TryEnqueue(() => ListViewExtensions.ReapplyAlternateColors(SelectedFilesListView));
    }

    private void DeleteFileButton_OnClick(object sender, RoutedEventArgs e)
    {
        var selectedFile = (SelectedFile)((Button)sender).DataContext;
        ViewModel.SelectedFiles.Remove(selectedFile);
    }
}