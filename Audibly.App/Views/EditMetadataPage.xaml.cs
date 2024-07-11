// Author: rstewa · https://github.com/rstewa
// Created: 07/02/2024
// Updated: 07/03/2024

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Windows.Storage.Pickers;
using ATL;
using Audibly.App.ViewModels;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using WinRT.Interop;

namespace Audibly.App.Views;

public sealed partial class EditMetadataPage : Page
{
    public EditMetadataPage()
    {
        InitializeComponent();
    }

    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private Track Track { get; set; }
    private AudiobookViewModel ViewModel { get; set; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ViewModel = (AudiobookViewModel)e.Parameter;
        Track = new Track(ViewModel.FilePath);

        // Use reflection to get all properties of the Track object
        var properties = Track.GetType().GetProperties();

        // observable collection
        var trackProperties = new ObservableCollection<TrackProperty>();

        // Iterate over each property
        foreach (var property in properties)
        {
            
            if (property.Name == "EmbeddedPictures")
            {
                // var embeddedPictures = property.GetValue(Track) as List<PictureInfo>;
                // var imageBytes = embeddedPictures.FirstOrDefault()?.PictureData;
            }

            var trackProperty = new TrackProperty
            {
                Name = $"{property.Name}:",
                Value = property.GetValue(Track)?.ToString() ?? "N/A"
            };
            
            trackProperties.Add(trackProperty);
        }

        TrackPropertiesListView.ItemsSource = trackProperties;
        TrackPropertiesListView.IsEnabled = false;

        base.OnNavigatedTo(e);
    }

    private async void EditCoverImageButton_Click(object sender, RoutedEventArgs e)
    {
        // Open file picker
        var filePicker = new FileOpenPicker();
        var window = App.Window;
        var hWnd = WindowNative.GetWindowHandle(window);
        InitializeWithWindow.Initialize(filePicker, hWnd);

        filePicker.ViewMode = PickerViewMode.Thumbnail;
        filePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
        filePicker.FileTypeFilter.Add(".jpg");
        filePicker.FileTypeFilter.Add(".jpeg");
        filePicker.FileTypeFilter.Add(".png");

        var file = await filePicker.PickSingleFileAsync();
        if (file != null)
        {
            // Get the file path
            var filePath = file.Path;

            // Write the cover image to the app data folder
            // get directory path
            var path = Path.GetFileName(Path.GetDirectoryName(ViewModel.CoverImagePath));

            if (path == null)
            {
                // log the error
                App.ViewModel.LoggingService.LogError(
                    new Exception("Could not get the directory path for the cover image"));
                App.ViewModel.EnqueueNotification(new Notification
                {
                    Message = "Could not get the directory path for the cover image.",
                    Severity = InfoBarSeverity.Error
                });
                return;
            }

            var (coverImagePath, thumbnailPath) =
                await App.ViewModel.AppDataService.WriteCoverImageAsync(path, filePath);

            // var viewModel2 = App.ViewModel.Audiobooks.FirstOrDefault(x => x.FilePath == ViewModel.FilePath);

            _ = _dispatcherQueue.EnqueueAsync(() =>
            {
                ViewModel.CoverImagePath = coverImagePath;
                ViewModel.ThumbnailPath = thumbnailPath;
            });
            
            await ViewModel.SaveAsync();
        }
    }

    private void CoverImageTile_OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        BlackOverlayGrid.Visibility = Visibility.Visible;
    }

    private void CoverImageTile_OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        BlackOverlayGrid.Visibility = Visibility.Collapsed;
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        TrackPropertiesListView.IsEnabled = true;
        EditButton.Visibility = Visibility.Collapsed;
        SaveButton.Visibility = Visibility.Visible;
        CancelButton.Visibility = Visibility.Visible;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        TrackPropertiesListView.IsEnabled = false;
        EditButton.Visibility = Visibility.Visible;
        SaveButton.Visibility = Visibility.Collapsed;
        CancelButton.Visibility = Visibility.Collapsed;
    }
}

public class TrackProperty : BindableBase
{
    private string _name;

    public string Name
    {
        get => _name;
        set => Set(ref _name, value);
    }

    private string _value;

    public string Value
    {
        get => _value;
        set => Set(ref _value, value);
    }
}