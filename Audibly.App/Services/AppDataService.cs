// Author: rstewa · https://github.com/rstewa
// Updated: 06/09/2025

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using ATL;
using Audibly.App.Helpers;
using Audibly.App.Services.Interfaces;
using Audibly.App.ViewModels;
using Audibly.Models;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;

namespace Audibly.App.Services;

public class AppDataService : IAppDataService
{
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private static StorageFolder StorageFolder => ApplicationData.Current.LocalFolder;

    /// <summary>
    ///     Gets the app-wide ViewModel instance.
    /// </summary>
    public MainViewModel ViewModel => App.ViewModel;

    #region IAppDataService Members

    public async Task<Tuple<string, string>> WriteCoverImageAsync(string path, byte[]? imageBytes)
    {
        try
        {
            string coverImagePath;
            var bookAppdataDir = await StorageFolder.CreateFolderAsync(path,
                CreationCollisionOption.OpenIfExists);

            if (imageBytes == null)
            {
                var coverImage = await AssetHelper.GetAssetFileAsync("DefaultCoverImage.png");
                await coverImage.CopyAsync(bookAppdataDir, "CoverImage.png", NameCollisionOption.ReplaceExisting);
                coverImagePath = coverImage.Path;
            }
            else
            {
                var coverImage =
                    await bookAppdataDir.CreateFileAsync("CoverImage.png", CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteBytesAsync(coverImage, imageBytes);
                coverImagePath = coverImage.Path;
            }

            // create 400x400 thumbnail
            var thumbnailPath = Path.Combine(bookAppdataDir.Path, "Thumbnail.jpeg");
            var result = await ShrinkAndSaveAsync(coverImagePath, thumbnailPath, 400, 400);
            if (!result) thumbnailPath = coverImagePath; // use full size image if thumbnail creation fails

            // leaving this commented out for now because it increases the import time an absurd amount
            // create .ico file
            // var coverImagePath = Path.Combine(bookAppdataDir.Path, "CoverImage.png");
            // FolderIcon.SetFolderIcon(coverImagePath, bookAppdataDir.Path);

            return new Tuple<string, string>(coverImagePath, thumbnailPath);
        }
        catch (Exception e)
        {
            App.ViewModel.LoggingService.LogError(e, true);
            return new Tuple<string, string>(string.Empty, string.Empty);
        }
    }

    public async Task DeleteCoverImageAsync(string path)
    {
        // note: the following code is only needed if I re-enable the .ico creation
        // var dir = Path.GetDirectoryName(path);
        // FolderIcon.ResetFolderAttributes(dir);
        // FolderIcon.DeleteIcon(dir);

        try
        {
            var dirName = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(dirName)) return;

            var folder = await StorageFolder.GetFolderFromPathAsync(dirName);

            // Try to delete all files in the folder first
            var files = await folder.GetFilesAsync();
            foreach (var file in files)
                try
                {
                    await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
                catch (Exception fileEx)
                {
                    App.ViewModel.LoggingService.LogError(fileEx);
                }

            // Now try to delete the empty folder
            await folder.DeleteAsync(StorageDeleteOption.PermanentDelete);
        }
        catch (Exception e)
        {
            App.ViewModel.LoggingService.LogError(e, true);
        }
    }

    public async Task DeleteCoverImagesAsync(List<string> paths, Func<int, int, string, Task> progressCallback)
    {
        for (var i = 0; i < paths.Count; i++)
        {
            await DeleteCoverImageAsync(paths[i]);
            await progressCallback(i, paths.Count, Path.GetFileName(paths[i]));
        }
    }

    public async Task WriteMetadataAsync(string path, Track track)
    {
        var bookAppdataDir = await StorageFolder.CreateFolderAsync(path,
            CreationCollisionOption.OpenIfExists);
        var json = JsonSerializer.Serialize(track, new JsonSerializerOptions { WriteIndented = true });
        await FileIO.WriteTextAsync(
            await bookAppdataDir.CreateFileAsync("Metadata.json", CreationCollisionOption.ReplaceExisting), json);
    }

    public async Task ExportMetadataAsync(List<SourceFile> sourceFiles)
    {
        try
        {
            // Create a list to hold all track metadata
            var tracks = new List<Track>();

            // Process each source file
            foreach (var sourceFile in sourceFiles)
            {
                var track = new Track(sourceFile.FilePath);
                tracks.Add(track);
            }

            // Serialize the entire collection
            var json = JsonSerializer.Serialize(tracks, new JsonSerializerOptions { WriteIndented = true });

            var file = ViewModel.FileDialogService.SaveFileDialog("Metadata.json",
                new List<string> { ".json" }, PickerLocationId.DocumentsLibrary);
            if (file == null) return; // User cancelled
            // Write the JSON to the file
            await FileIO.WriteTextAsync(file, json);

            // show notification
            App.ViewModel.EnqueueNotification(new Notification
            {
                Message = $"Metadata exported to {file.Name}", Severity = InfoBarSeverity.Success
            });


            // Uncomment the following lines if you want to use a dispatcher queue for UI updates

            // await _dispatcherQueue.EnqueueAsync(async () =>
            // {
            //     // Let the user choose where to save the combined metadata file
            //     var savePicker = new FileSavePicker
            //     {
            //         SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            //         SuggestedFileName = "Combined Metadata.json",
            //         FileTypeChoices = { { "JSON", new List<string> { ".json" } } }
            //     };
            //     var file = await savePicker.PickSaveFileAsync();
            //     if (file == null) return; // User cancelled
            //
            //     await FileIO.WriteTextAsync(file, json);
            // });
        }
        catch (Exception e)
        {
            App.ViewModel.LoggingService.LogError(e, true);
        }
    }

    #endregion

    // from: https://stackoverflow.com/questions/26486671/how-to-resize-an-image-maintaining-the-aspect-ratio-in-c-sharp
    private async Task<bool> ShrinkAndSaveAsync(string path, string savePath, int maxHeight, int maxWidth)
    {
        try
        {
            using var image = await Image.LoadAsync(path);
            if (ResizeNeeded(image.Height, image.Width, maxHeight, maxWidth, out var newHeight, out var newWidth))
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(newWidth, newHeight)
                }));

            await image.SaveAsync(savePath);

            return true;
        }
        catch (Exception e)
        {
            App.ViewModel.LoggingService.LogError(e, true);
            return false;
        }
    }

    private bool ResizeNeeded(int height, int width, int maxHeight, int maxWidth, out int newHeight, out int newWidth)
    {
        // first use existing dimensions
        newHeight = height;
        newWidth = width;

        // if below max on both then do nothing
        if (height <= maxHeight && width <= maxWidth) return false;

        // naively check height first
        if (height > maxHeight)
        {
            // set down to max height
            newHeight = maxHeight;

            // calculate what new width would be
            var heightReductionRatio = maxHeight / height; // ratio of maxHeight:image.Height
            newWidth = width * heightReductionRatio; // apply ratio to image.Width
        }

        // does width need to be reduced? 
        // (this will also re-check width after shrinking by height dimension)
        if (newWidth > maxWidth)
        {
            // if so, re-calculate height to fit for maxWidth
            var widthReductionRatio =
                maxWidth / newWidth; // ratio of maxWidth:newWidth (height reduction ratio may have been applied)
            newHeight = maxHeight * widthReductionRatio; // apply new ratio to maxHeight to get final height
            newWidth = maxWidth;
        }

        // if we got here, resize needed and out vars have been set
        return true;
    }
}