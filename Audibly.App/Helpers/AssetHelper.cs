// Author: rstewa · https://github.com/rstewa
// Created: 10/14/2024
// Updated: 10/14/2024

using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace Audibly.App.Helpers;

public static class AssetHelper
{
    public static async Task<StorageFile> GetAssetFileAsync(string assetFileName)
    {
        try
        {
            var uri = new Uri($"ms-appx:///Assets/{assetFileName}");
            var assetFile = await StorageFile.GetFileFromApplicationUriAsync(uri);
            return assetFile;
        }
        catch (Exception ex)
        {
            // Handle exceptions as needed
            throw new Exception("Failed to get asset file", ex);
        }
    }
}