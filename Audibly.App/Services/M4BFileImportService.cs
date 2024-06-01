// Author: rstewa · https://github.com/rstewa
// Created: 4/15/2024
// Updated: 6/1/2024

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATL;
using Audibly.App.Services.Interfaces;
using Audibly.Models;
using AutoMapper;
using Sharpener.Extensions;
using ChapterInfo = Audibly.Models.ChapterInfo;

namespace Audibly.App.Services;

public class M4BFileImportService : IImportFiles
{
    public event IImportFiles.ImportCompletedHandler? ImportCompleted;

    private static IMapper _mapper;

    public M4BFileImportService()
    {
        _mapper = new MapperConfiguration(cfg => { cfg.CreateMap<ATL.ChapterInfo, ChapterInfo>(); }).CreateMapper();
    }

    // TODO: need a better way of checking if a file is one we have already imported
    public async Task ImportDirectoryAsync(string path, CancellationToken cancellationToken,
        Func<int, int, string, bool, Task> progressCallback)
    {
        var didFail = false;
        var files = Directory.GetFiles(path, "*.m4b", SearchOption.AllDirectories);
        var numberOfFiles = files.Length;
        var filesList = files.AsList();

        foreach (var file in files)
        {
            // Check if cancellation was requested
            cancellationToken.ThrowIfCancellationRequested();

            var audiobook = await CreateAudiobook(file);

            if (audiobook == null) didFail = true;

            if (audiobook != null)
            {
                // insert the audiobook into the database
                var result = await App.Repository.Audiobooks.UpsertAsync(audiobook);
                if (result == null) didFail = true;
            }

            var title = audiobook?.Title ?? Path.GetFileNameWithoutExtension(file);

            // report progress
            await progressCallback(filesList.IndexOf(file), numberOfFiles, title, didFail);

            didFail = false;
        }

        ImportCompleted?.Invoke();
    }

    public async Task ImportFileAsync(string path, CancellationToken cancellationToken,
        Func<int, int, string, bool, Task> progressCallback)
    {
        // Check if cancellation was requested
        cancellationToken.ThrowIfCancellationRequested();

        var didFail = false;
        var audiobook = await CreateAudiobook(path);

        if (audiobook == null) didFail = true;

        // insert the audiobook into the database
        if (audiobook != null)
        {
            var result = await App.Repository.Audiobooks.UpsertAsync(audiobook);
            if (result == null) didFail = true;
        }

        var title = audiobook?.Title ?? Path.GetFileNameWithoutExtension(path);

        // report progress
        // NOTE: keeping this bc this function will be used in the future to import 1-to-many files
        await progressCallback(1, 1, title, didFail);

        ImportCompleted?.Invoke();
    }

    private static async Task<Audiobook?> CreateAudiobook(string path)
    {
        Track track;
        try
        {
            track = new Track(path);
        }
        catch (Exception e)
        {
            // log the error
            App.ViewModel.LoggingService.LogError(e);
            return null;
        }

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
            CurrentChapterIndex = 0,
            Chapters = []
        };

        // TODO: check if the audiobook already exists in the database


        // save the cover image somewhere
        var imageBytes = track.EmbeddedPictures.FirstOrDefault()?.PictureData;

        var dir = $"{Path.GetFileNameWithoutExtension(path)} [{track.Artist}]";

        // write the metadata to a json file
        await App.ViewModel.AppDataService.WriteMetadataAsync(dir, track);

        (audiobook.CoverImagePath, audiobook.ThumbnailPath) =
            await App.ViewModel.AppDataService.WriteCoverImageAsync(dir, imageBytes);

        // read in the chapters
        var chapterIndex = 0;
        foreach (var ch in track.Chapters)
        {
            var tmp = _mapper.Map<ChapterInfo>(ch);
            tmp.Index = chapterIndex++;
            audiobook.Chapters.Add(tmp);
        }

        if (audiobook.Chapters.Count == 0)
            // create a single chapter for the entire book
            audiobook.Chapters.Add(new ChapterInfo
            {
                StartTime = 0,
                EndTime = Convert.ToUInt32(audiobook.Duration * 1000),
                StartOffset = 0,
                EndOffset = 0,
                UseOffset = false,
                Title = audiobook.Title,
                Index = 0
            });

        return audiobook;
    }
}