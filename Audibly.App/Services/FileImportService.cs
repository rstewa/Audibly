// Author: rstewa · https://github.com/rstewa
// Created: 04/15/2024
// Updated: 10/17/2024

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

public class FileImportService : IImportFiles
{
    public event IImportFiles.ImportCompletedHandler? ImportCompleted;

    private static IMapper _mapper;

    public FileImportService()
    {
        _mapper = new MapperConfiguration(cfg => { cfg.CreateMap<ATL.ChapterInfo, ChapterInfo>(); }).CreateMapper();
    }

    // TODO: need a better way of checking if a file is one we have already imported
    public async Task ImportDirectoryAsync(string path, CancellationToken cancellationToken,
        Func<int, int, string, bool, Task> progressCallback)
    {
        var didFail = false;

        var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
            .Where(file => file.EndsWith(".m4b", StringComparison.OrdinalIgnoreCase) ||
                           file.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var numberOfFiles = files.Count;

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

    public async Task ImportFilesAsync(string[] paths, CancellationToken cancellationToken,
        Func<int, int, string, bool, Task> progressCallback)
    {
        var didFail = false;

        // todo: need to see if we can call progressCallback from the CreateAudiobook function
        var numberOfFiles = 1; // paths.Length;

        // Check if cancellation was requested
        cancellationToken.ThrowIfCancellationRequested();

        var audiobook = await CreateAudiobookFromMultipleFiles(paths);

        if (audiobook == null) didFail = true;

        if (audiobook != null)
        {
            // insert the audiobook into the database
            var result = await App.Repository.Audiobooks.UpsertAsync(audiobook);
            if (result == null) didFail = true;
        }

        var title = audiobook?.Title ?? Path.GetFileNameWithoutExtension(paths.First());

        // report progress
        await progressCallback(1, numberOfFiles, title, didFail);

        didFail = false;

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

    private static async Task<Audiobook?> CreateAudiobookFromMultipleFiles(string[] paths)
    {
        try
        {
            var audiobook = new Audiobook
            {
                CurrentSourceFileIndex = 0,
                SourcePaths = [],
                PlaybackSpeed = 1.0,
                Volume = 1.0
            };

            var sourceFileIndex = 0;
            var chapterIndex = 0;
            foreach (var path in paths)
            {
                var track = new Track(path);

                // check if this is the 1st file
                if (audiobook.SourcePaths.Count == 0)
                {
                    audiobook.Title = track.Title;
                    audiobook.Composer = track.Composer;
                    audiobook.Author = track.Artist;
                    audiobook.Description = track.AdditionalFields.TryGetValue("\u00A9des", out var value)
                        ? value
                        : track.Comment;
                    audiobook.ReleaseDate = track.Date;
                }

                var sourceFile = new SourceFile
                {
                    Index = sourceFileIndex++,
                    FilePath = path,
                    Duration = track.Duration,
                    CurrentTimeMs = 0
                };

                audiobook.SourcePaths.Add(sourceFile);

                // read in the chapters
                foreach (var ch in track.Chapters)
                {
                    var tmp = _mapper.Map<ChapterInfo>(ch);
                    tmp.Index = chapterIndex++;
                    tmp.ParentSourceFileIndex = sourceFile.Index;
                    audiobook.Chapters.Add(tmp);
                }

                if (track.Chapters.Count == 0)
                    // create a single chapter for the entire book
                    audiobook.Chapters.Add(new ChapterInfo
                    {
                        StartTime = 0,
                        EndTime = Convert.ToUInt32(audiobook.SourcePaths[sourceFileIndex - 1].Duration * 1000),
                        StartOffset = 0,
                        EndOffset = 0,
                        UseOffset = false,
                        Title = audiobook.Title,
                        Index = chapterIndex++,
                        ParentSourceFileIndex = sourceFile.Index
                    });
            }

            // get duration of the entire audiobook
            audiobook.Duration = audiobook.SourcePaths.Sum(x => x.Duration);

            // save the cover image somewhere
            var imageBytes = new Track(paths.First()).EmbeddedPictures.FirstOrDefault()?.PictureData;

            // note: for now using a GUID to prevent the path from being too long and causing the import to fail
            var dir = Guid.NewGuid().ToString();

            // todo: do i want to write the metadata to a json file here?
            // write the metadata to a json file
            // await App.ViewModel.AppDataService.WriteMetadataAsync(dir, track);

            (audiobook.CoverImagePath, audiobook.ThumbnailPath) =
                await App.ViewModel.AppDataService.WriteCoverImageAsync(dir, imageBytes);

            // combine the chapters from all the files
            // audiobook.Chapters = audiobook.SourcePaths.SelectMany(x => x.Chapters).ToList();
            audiobook.CurrentChapterIndex = 0;

            return audiobook;
        }
        catch (Exception e)
        {
            // log the error
            App.ViewModel.LoggingService.LogError(e);
            return null;
        }
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
                Index = 0,
                FilePath = path,
                Duration = track.Duration,
                CurrentTimeMs = 0
                // CurrentChapterIndex = 0,
                // Chapters = []
            };

            var audiobook = new Audiobook
            {
                CurrentSourceFileIndex = 0,
                Title = track.Title,
                Composer = track.Composer,
                Duration = track.Duration,
                Author = track.Artist,
                Description =
                    track.Description, // track.AdditionalFields.TryGetValue("\u00A9des", out var value) ? value : track.Comment,
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

            // var chapters = audiobook.SourcePaths.First().Chapters;

            // read in the chapters
            var chapterIndex = 0;
            foreach (var ch in track.Chapters)
            {
                var tmp = _mapper.Map<ChapterInfo>(ch);
                tmp.Index = chapterIndex++;
                tmp.ParentSourceFileIndex = sourceFile.Index;
                audiobook.Chapters.Add(tmp);
            }

            if (audiobook.Chapters.Count == 0)
                // create a single chapter for the entire book
                audiobook.Chapters.Add(new ChapterInfo
                {
                    StartTime = 0,
                    EndTime = Convert.ToUInt32(audiobook.SourcePaths.First().Duration * 1000),
                    StartOffset = 0,
                    EndOffset = 0,
                    UseOffset = false,
                    Title = audiobook.Title,
                    Index = 0,
                    ParentSourceFileIndex = sourceFile.Index
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