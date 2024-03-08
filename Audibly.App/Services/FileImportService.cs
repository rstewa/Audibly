// Author: rstewa
// Created: 3/6/2024
// Updated: 3/6/2024

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using ATL;
using Audibly.Models;

namespace Audibly.App.Services;

public class FileImportService : IImportFiles
{
    private static StorageFolder StorageFolder => ApplicationData.Current.LocalFolder;

    public async Task ImportAsync(string path)
    {
        var files = Directory.GetFiles(path, "*.m4b", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            var track = new Track(file);
            var bookAppdataDir = await StorageFolder.CreateFolderAsync(
                $"{Path.GetFileNameWithoutExtension(file)} [{track.Artist}]", CreationCollisionOption.OpenIfExists);
            var audiobook = new Audiobook
            {
                Title = track.Title,
                Author = track.Artist,
                Description = track.AdditionalFields.TryGetValue("\u00A9des", out var value) ? value : track.Comment,
                FilePath = file,
                Duration = track.Duration,
                CurrentTimeMs = 0,
                PlaybackSpeed = 1.0,
                Volume = 1.0,
                CurrentChapter = null,
                Chapters = []
            };

            // save the cover image somewhere
            var imageBytes = track.EmbeddedPictures.FirstOrDefault()?.PictureData;
            var coverImage =
                await bookAppdataDir.CreateFileAsync("CoverImage.png", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteBytesAsync(coverImage, imageBytes);

            audiobook.CoverImagePath = coverImage.Path;

            // insert the audiobook into the database
            await App.Repository.Audiobooks.UpsertAsync(audiobook);
        }
    }
}