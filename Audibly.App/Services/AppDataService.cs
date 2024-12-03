// Author: rstewa · https://github.com/rstewa
// Created: 4/15/2024
// Updated: 6/11/2024

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using ATL;
using Audibly.App.Helpers;
using Audibly.App.Services.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Audibly.App.Services;

public class AppDataService : IAppDataService
{
    private static StorageFolder StorageFolder => ApplicationData.Current.LocalFolder;

    #region IAppDataService Members

    public async Task<Tuple<string, string>> WriteCoverImageAsync(string path, byte[]? imageBytes)
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
        if (!result) thumbnailPath = string.Empty; // return empty string if thumbnail creation failed

        // leaving this commented out for now because it increases the import time an absurd amount
        // create .ico file
        // var coverImagePath = Path.Combine(bookAppdataDir.Path, "CoverImage.png");
        // FolderIcon.SetFolderIcon(coverImagePath, bookAppdataDir.Path);

        return new Tuple<string, string>(coverImagePath, thumbnailPath);
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

            await folder.DeleteAsync();
        }
        catch (Exception e)
        {
            App.ViewModel.LoggingService.LogError(e);
        }
    }

    public async Task DeleteCoverImagesAsync(List<string> paths, Func<int, int, string, Task> progressCallback)
    {
        for (var i = 0; i < paths.Count; i++)
        {
            await DeleteCoverImageAsync(paths[i]);
            await progressCallback(i, paths.Count, Path.GetFileName(paths[i]));
        }

        ImportCompleted?.Invoke();
    }

    public async Task WriteMetadataAsync(string path, Track track)
    {
        var bookAppdataDir = await StorageFolder.CreateFolderAsync(path,
            CreationCollisionOption.OpenIfExists);
        var json = JsonSerializer.Serialize(track, new JsonSerializerOptions { WriteIndented = true });
        await FileIO.WriteTextAsync(
            await bookAppdataDir.CreateFileAsync("Metadata.json", CreationCollisionOption.ReplaceExisting), json);
    }

    #endregion

    // todo: need a cleaner way to handle this
    public event IImportFiles.ImportCompletedHandler? ImportCompleted;

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
            App.ViewModel.LoggingService.LogError(e);
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