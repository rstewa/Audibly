using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Audibly.App.ViewModels;
using Audibly.Models;
using Audibly.Repository.Interfaces;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.Services.Transcription;

/// <summary>
///     Owns the background transcription worker: picks chapters, drives
///     <see cref="ChapterTranscriber" />, persists status transitions, unloads the model
///     after 60 s idle, and surfaces progress via events (raised on the worker thread —
///     consumers must dispatch).
/// </summary>
public class TranscriptionCoordinator : IDisposable
{
    private const int IdleUnloadDelayMs = 60_000;

    private readonly IAudiblyRepository _repository;
    private readonly TranscriptionModelService _modelService;
    private readonly ISpeechToTextBackend _backend;
    private readonly IPcmAudioExtractor _extractor;
    private readonly ChapterTranscriber _transcriber;

    private readonly ConcurrentQueue<Guid> _manualQueue = new();
    private readonly object _workerLock = new();
    private readonly HashSet<Guid> _failureNotifiedBooks = [];

    private Task? _worker;
    private CancellationTokenSource? _workerCts;
    private Timer? _idleTimer;
    private bool? _extractorAvailable;

    public TranscriptionCoordinator(IAudiblyRepository repository, TranscriptionModelService modelService,
        ISpeechToTextBackend backend, IPcmAudioExtractor extractor)
    {
        _repository = repository;
        _modelService = modelService;
        _backend = backend;
        _extractor = extractor;
        _transcriber = new ChapterTranscriber(repository, backend, extractor);

        _modelService.StateChanged += () =>
        {
            if (_modelService.State == SpeechModelState.Ready) KickWorker();
        };
    }

    /// <summary>
    ///     (book, chapterIndex, status, progressPercent) — raised on the worker thread.
    /// </summary>
    public event Action<Guid, int, TranscriptStatus, int>? StatusChanged;

    /// <summary>
    ///     (book, chapterIndex, newly persisted segments) — raised on the worker thread.
    /// </summary>
    public event Action<Guid, int, IReadOnlyList<TranscriptSegment>>? SegmentsFlushed;

    public bool IsSupportedPlatform => _backend.IsSupportedOnThisDevice &&
                                       (_extractorAvailable ??= _extractor.IsAvailable);

    public bool CanTranscribe => IsSupportedPlatform && _modelService.State == SpeechModelState.Ready;

    /// <summary>
    ///     Startup: clean temp files, recover chapters left InProgress by a crash, start work.
    /// </summary>
    public async Task InitializeAsync()
    {
        LibVlcPcmAudioExtractor.SweepTempFiles();

        try
        {
            var recovered = await _repository.Transcripts.ResetInterruptedAsync();
            if (recovered > 0)
                App.ViewModel.LoggingService.Log($"Transcription: re-queued {recovered} interrupted chapter(s).");
        }
        catch (Exception e)
        {
            App.ViewModel.LoggingService.LogError(e, true);
        }

        KickWorker();
    }

    /// <summary>
    ///     Manual "Transcribe now": queues the whole book (retrying Failed chapters), even
    ///     when automatic transcription is off.
    /// </summary>
    public void RequestBook(Guid audiobookId)
    {
        if (!_manualQueue.Contains(audiobookId)) _manualQueue.Enqueue(audiobookId);
        KickWorker();
    }

    public void OnSettingsChanged()
    {
        KickWorker();
    }

    /// <summary>
    ///     Stops the worker and frees the model + decoder (used before deleting the model).
    /// </summary>
    public async Task StopAndUnloadAsync()
    {
        Task? worker;
        lock (_workerLock)
        {
            _workerCts?.Cancel();
            worker = _worker;
        }

        if (worker != null)
            try
            {
                await worker;
            }
            catch
            {
                // worker exceptions are already logged
            }

        _backend.Unload();
        _extractor.Unload();
    }

    /// <summary>
    ///     App shutdown: stop scheduling new windows. Interrupted chapters are recovered by
    ///     <see cref="InitializeAsync" /> on the next launch.
    /// </summary>
    public void Shutdown()
    {
        lock (_workerLock)
        {
            _workerCts?.Cancel();
        }
    }

    private void KickWorker()
    {
        if (!CanTranscribe) return;

        lock (_workerLock)
        {
            _idleTimer?.Dispose();
            _idleTimer = null;

            if (_worker is { IsCompleted: false }) return;

            _workerCts?.Dispose();
            _workerCts = new CancellationTokenSource();
            var token = _workerCts.Token;
            _worker = Task.Run(() => WorkerLoopAsync(token), CancellationToken.None);
        }
    }

    private async Task WorkerLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && CanTranscribe)
            {
                if (!_manualQueue.TryDequeue(out var bookId)) break;
                await ProcessBookAsync(bookId, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // shutdown / stop
        }
        catch (Exception e)
        {
            App.ViewModel.LoggingService.LogError(e, true);
        }
        finally
        {
            ScheduleIdleUnload();
        }
    }

    private async Task ProcessBookAsync(Guid bookId, CancellationToken cancellationToken)
    {
        var book = await _repository.Audiobooks.GetAsync(bookId);
        if (book == null) return;

        await _repository.Transcripts.EnsureStatusRowsAsync(bookId,
            book.Chapters.Select(c => (c.Index, c.ParentSourceFileIndex)));
        var statuses = (await _repository.Transcripts.GetStatusesAsync(bookId))
            .ToDictionary(s => s.ChapterIndex);

        var didWork = false;
        var anyFailed = false;

        foreach (var chapter in book.Chapters.OrderBy(c => c.Index))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!CanTranscribe) return;

            if (statuses.TryGetValue(chapter.Index, out var status) &&
                status.Status == TranscriptStatus.Completed)
                continue;

            didWork = true;
            if (!await TranscribeChapterAsync(book, chapter, cancellationToken)) anyFailed = true;
        }

        if (didWork && !anyFailed)
            App.ViewModel.EnqueueNotification(new Notification
            {
                Message = $"Finished transcribing \"{book.Title}\".",
                Severity = InfoBarSeverity.Success
            });
    }

    /// <summary>
    ///     Runs one chapter; returns false when it failed (already logged and persisted).
    /// </summary>
    private async Task<bool> TranscribeChapterAsync(Audiobook book, ChapterInfo chapter,
        CancellationToken cancellationToken)
    {
        var modelId = _backend.Model.Id;

        var sourceFile = book.SourcePaths.FirstOrDefault(s => s.Index == chapter.ParentSourceFileIndex);
        if (sourceFile == null)
        {
            await _repository.Transcripts.UpdateStatusAsync(book.Id, chapter.Index, TranscriptStatus.Failed, 0,
                $"No source file with index {chapter.ParentSourceFileIndex}.", modelId);
            StatusChanged?.Invoke(book.Id, chapter.Index, TranscriptStatus.Failed, 0);
            return false;
        }

        await _backend.EnsureLoadedAsync(_modelService.ModelDirectory, cancellationToken);

        await _repository.Transcripts.DeleteChapterSegmentsAsync(book.Id, chapter.Index);
        await _repository.Transcripts.UpdateStatusAsync(book.Id, chapter.Index, TranscriptStatus.InProgress, 0,
            null, modelId);
        StatusChanged?.Invoke(book.Id, chapter.Index, TranscriptStatus.InProgress, 0);

        var job = new ChapterTranscriptionJob(book.Id, chapter.Index, chapter.ParentSourceFileIndex,
            sourceFile.FilePath, chapter.StartTime, chapter.EndTime, sourceFile.Duration * 1000, modelId);

        try
        {
            await _transcriber.RunAsync(job,
                pct => StatusChanged?.Invoke(book.Id, chapter.Index, TranscriptStatus.InProgress, pct),
                segments => SegmentsFlushed?.Invoke(book.Id, chapter.Index, segments),
                ShouldPreemptCurrentChapter,
                cancellationToken);

            StatusChanged?.Invoke(book.Id, chapter.Index, TranscriptStatus.Completed, 100);
            return true;
        }
        catch (Exception e) when (e is OperationCanceledException or TranscriptionPreemptedException)
        {
            // yield cleanly: purge partial rows and put the chapter back in line
            await _repository.Transcripts.DeleteChapterSegmentsAsync(book.Id, chapter.Index);
            await _repository.Transcripts.UpdateStatusAsync(book.Id, chapter.Index, TranscriptStatus.Queued, 0,
                null, modelId);
            StatusChanged?.Invoke(book.Id, chapter.Index, TranscriptStatus.Queued, 0);
            throw;
        }
        catch (Exception e)
        {
            App.ViewModel.LoggingService.LogError(e, true);
            await _repository.Transcripts.DeleteChapterSegmentsAsync(book.Id, chapter.Index);
            await _repository.Transcripts.UpdateStatusAsync(book.Id, chapter.Index, TranscriptStatus.Failed, 0,
                e.Message, modelId);
            StatusChanged?.Invoke(book.Id, chapter.Index, TranscriptStatus.Failed, 0);

            if (_failureNotifiedBooks.Add(book.Id))
                App.ViewModel.EnqueueNotification(new Notification
                {
                    Message = $"Transcription of \"{book.Title}\" failed: {e.Message}",
                    Severity = InfoBarSeverity.Warning
                });
            return false;
        }
    }

    /// <summary>
    ///     Slice hook for the playback-aware scheduler; the manual queue never preempts.
    /// </summary>
    private bool ShouldPreemptCurrentChapter()
    {
        return false;
    }

    private void ScheduleIdleUnload()
    {
        lock (_workerLock)
        {
            _idleTimer?.Dispose();
            _idleTimer = new Timer(_ =>
            {
                lock (_workerLock)
                {
                    if (_worker is { IsCompleted: false }) return;
                    _backend.Unload();
                    _extractor.Unload();
                }
            }, null, IdleUnloadDelayMs, Timeout.Infinite);
        }
    }

    public void Dispose()
    {
        Shutdown();
        _idleTimer?.Dispose();
        _backend.Dispose();
        _extractor.Unload();
    }
}
