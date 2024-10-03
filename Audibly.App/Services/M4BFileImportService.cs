// Author: rstewa · https://github.com/rstewa
// Created: 04/15/2024
// Updated: 10/03/2024

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATL;
using Audibly.App.Services.Interfaces;
using Audibly.App.ViewModels;
using Audibly.Models;
using AutoMapper;
using Microsoft.UI.Xaml.Controls;
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
        try
        {
            var track = new Track(path);

            var existingAudioBook = await App.Repository.Audiobooks.GetAsync(track.Title, track.Artist, track.Composer);
            if (existingAudioBook != null)
            {
                // log the error
                App.ViewModel.LoggingService.LogError(new Exception("Audiobook already exists in the database"));
                App.ViewModel.EnqueueNotification(new Notification
                {
                    Message = "Audiobook is already in the library.",
                    Severity = InfoBarSeverity.Warning
                });
                return null;
            }

            var sourceFile = new SourceFile
            {
                FilePath = path,
                Duration = track.Duration,
                CurrentTimeMs = 0,
                CurrentChapterIndex = 0,
                Chapters = []
            };

            var audiobook = new Audiobook
            {
                CurrentSourceFileIndex = 0,
                Title = track.Title,
                Composer = track.Composer,
                Author = track.Artist,
                Description = track.AdditionalFields.TryGetValue("\u00A9des", out var value) ? value : track.Comment,
                FilePath = path,
                PlaybackSpeed = 1.0,
                ReleaseDate = track.Date,
                Volume = 1.0,
                SourcePaths =
                [
                    sourceFile
                ]
            };

            // TODO: check if the audiobook already exists in the database

            // save the cover image somewhere
            var imageBytes = track.EmbeddedPictures.FirstOrDefault()?.PictureData;

            // note: for now using a GUID to prevent the path from being too long and causing the import to fail
            var dir = Guid.NewGuid().ToString();

            // write the metadata to a json file
            // todo: is this killing the import time?
            await App.ViewModel.AppDataService.WriteMetadataAsync(dir, track);

            (audiobook.CoverImagePath, audiobook.ThumbnailPath) =
                await App.ViewModel.AppDataService.WriteCoverImageAsync(dir, imageBytes);

            var chapters = audiobook.SourcePaths.First().Chapters;

            // read in the chapters
            var chapterIndex = 0;
            foreach (var ch in track.Chapters)
            {
                var tmp = _mapper.Map<ChapterInfo>(ch);
                tmp.Index = chapterIndex++;
                chapters.Add(tmp);
            }

            if (chapters.Count == 0)
                // create a single chapter for the entire book
                chapters.Add(new ChapterInfo
                {
                    StartTime = 0,
                    EndTime = Convert.ToUInt32(audiobook.SourcePaths.First().Duration * 1000),
                    StartOffset = 0,
                    EndOffset = 0,
                    UseOffset = false,
                    Title = audiobook.Title,
                    Index = 0
                });

            return audiobook;
        }
        catch (Exception e)
        {
            // log the error
            App.ViewModel.LoggingService.LogError(e);
            return null;
        }
    }
}