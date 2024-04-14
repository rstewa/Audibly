// Author: rstewa · https://github.com/rstewa
// Created: 3/31/2024
// Updated: 4/13/2024

using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Audibly.App.Services.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Audibly.App.Services;

public class AppDataService : IAppDataService
{
    private static StorageFolder StorageFolder => ApplicationData.Current.LocalFolder;

    public async Task<Tuple<string, string>> WriteCoverImageAsync(string path, byte[]? imageBytes)
    {
        var bookAppdataDir = await StorageFolder.CreateFolderAsync(path,
            CreationCollisionOption.OpenIfExists);
        var coverImage =
            await bookAppdataDir.CreateFileAsync("CoverImage.jpeg", CreationCollisionOption.ReplaceExisting);
        await FileIO.WriteBytesAsync(coverImage, imageBytes);

        // create 400x400 thumbnail
        var thumbnailPath = Path.Combine(bookAppdataDir.Path, "Thumbnail.jpeg");
        await ShrinkAndSaveAsync(coverImage.Path, thumbnailPath, 400, 400);

        return new Tuple<string, string>(coverImage.Path, thumbnailPath);
    }

    public async Task DeleteCoverImageAsync(string path)
    {
        var folder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(path));
        await folder.DeleteAsync();
    }

    // from: https://stackoverflow.com/questions/26486671/how-to-resize-an-image-maintaining-the-aspect-ratio-in-c-sharp
    private async Task ShrinkAndSaveAsync(string path, string savePath, int maxHeight, int maxWidth)
    {
        using var image = await Image.LoadAsync(path);

        // check if resize is needed
        if (ResizeNeeded(image.Height, image.Width, maxHeight, maxWidth, out var newHeight, out var newWidth))
            // swap this part out if not using ImageSharp
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(newWidth, newHeight)
            }));

        await image.SaveAsync(savePath);
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