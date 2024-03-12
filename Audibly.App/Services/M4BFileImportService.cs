// Author: rstewa · https://github.com/rstewa
// Created: 3/6/2024
// Updated: 3/11/2024

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using ATL;
using Audibly.Models;
using Sharpener.Extensions;

namespace Audibly.App.Services;

public class M4BFileImportService : IImportFiles
{
    private static StorageFolder StorageFolder => ApplicationData.Current.LocalFolder;


    public async Task ImportDirectoryAsync(string path, Func<int, int, string, Task> progressCallback)
    {
        var files = Directory.GetFiles(path, "*.m4b", SearchOption.AllDirectories);
        var numberOfFiles = files.Length;
        var filesList = files.AsList();

        foreach (var file in files)
        {
            var audiobook = await CreateAudiobook(file);

            // insert the audiobook into the database
            await App.Repository.Audiobooks.UpsertAsync(audiobook);

            // TODO: insert the chapters into the database

            // report progress
            await progressCallback(filesList.IndexOf(file), numberOfFiles, audiobook.Title);
        }
    }

    public async Task ImportFileAsync(string path, Func<int, int, string, Task> progressCallback)
    {
        var audiobook = await CreateAudiobook(path);

        // insert the audiobook into the database
        await App.Repository.Audiobooks.UpsertAsync(audiobook);

        // TODO: insert the chapters into the database

        // report progress
        // NOTE: keeping this bc this function will be used in the future to import 1-to-many files
        await progressCallback(1, 1, audiobook.Title);
    }

    private static async Task<Audiobook> CreateAudiobook(string path)
    {
        var track = new Track(path);
        var bookAppdataDir = await StorageFolder.CreateFolderAsync(
            $"{Path.GetFileNameWithoutExtension(path)} [{track.Artist}]", CreationCollisionOption.OpenIfExists);
        var audiobook = new Audiobook
        {
            Title = track.Title,
            Composer = track.Composer,
            Author = track.Artist,
            Description = track.AdditionalFields.TryGetValue("\u00A9des", out var value) ? value : track.Comment,
            FilePath = path,
            Duration = track.Duration,
            CurrentTimeMs = 0,
            PlaybackSpeed = 1.0,
            ReleaseDate = track.Date,
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

        return audiobook;
    }
}