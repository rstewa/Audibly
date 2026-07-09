using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Audibly.App.Helpers;
using Audibly.App.ViewModels;
using Audibly.Models;
using Audibly.Repository.Interfaces;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.Services.Transcription;

/// <summary>
///     Owns the background transcription worker. One chapter is transcribed at a time,
///     re-picking after every chapter so priorities follow playback:
///     ① the playing book's current chapter, ② its later chapters, ③ its earlier chapters,
///     ④ manually requested books (works even with automatic scope Off, retries Failed),
///     ⑤ with scope "entire library": other unfinished books (most recently played first,
///     from their current chapter, wrapping), ⑥ finished books. Playback movement preempts
///     the in-flight chapter at the next window boundary when it is no longer the top pick.
///     The model and decoder are unloaded after 60 s idle. Events fire on the worker
///     thread — consumers must dispatch.
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
    private readonly ConcurrentDictionary<(Guid BookId, int ChapterIndex), TranscriptStatus> _statusCache = new();

    private Task? _worker;
    private CancellationTokenSource? _workerCts;
    private Timer? _idleTimer;
    private bool? _extractorAvailable;
    private volatile bool _playbackChanged;
    private volatile object? _deleteTargetBook;
    private (Guid BookId, int ChapterIndex)? _runningChapter;
    private AudiobookViewModel? _observedBook;

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

    /// <summary>
    ///     Raised when <see cref="ActivityDescription" /> changes — on the worker thread.
    /// </summary>
    public event Action? ActivityChanged;

    public string ActivityDescription { get; private set; } = "Idle";

    public bool IsSupportedPlatform => _backend.IsSupportedOnThisDevice &&
                                       (_extractorAvailable ??= _extractor.IsAvailable);

    public bool CanTranscribe => IsSupportedPlatform &&
                                 UserSettings.TranscriptionEnabled &&
                                 _modelService.State == SpeechModelState.Ready;

    /// <summary>
    ///     Startup: clean temp files, recover chapters left InProgress by a crash, hook
    ///     playback tracking, start work.
    /// </summary>
    public async Task InitializeAsync()
    {
        LibVlcPcmAudioExtractor.SweepTempFiles();

        try
        {
            var recovered = await _repository.Transcripts.ResetInterruptedAsync();
            if (recovered > 0)
                App.ViewModel.LoggingService.Log($"Transcription: re-queued {recovered} interrupted chapter(s).");

            foreach (var status in await _repository.Transcripts.GetAllStatusesAsync())
                _statusCache[(status.AudiobookId, status.ChapterIndex)] = status.Status;
        }
        catch (Exception e)
        {
            App.ViewModel.LoggingService.LogError(e, true);
        }

        App.PlayerViewModel.PropertyChanged += OnPlayerPropertyChanged;
        ObserveNowPlaying(App.PlayerViewModel.NowPlaying);

        KickWorker();
    }

    /// <summary>
    ///     Manual "Transcribe now": queues the whole book (retrying Failed chapters), even
    ///     when the automatic scope is Off.
    /// </summary>
    public void RequestBook(Guid audiobookId)
    {
        if (!_manualQueue.Contains(audiobookId)) _manualQueue.Enqueue(audiobookId);
        _failureNotifiedBooks.Remove(audiobookId);
        KickWorker();
    }

    public void OnSettingsChanged()
    {
        _playbackChanged = true; // re-evaluate the pick against the new scope
        KickWorker();
    }

    /// <summary>
    ///     True when the book has transcript status rows and every chapter is Completed.
    /// </summary>
    public bool IsBookFullyTranscribed(Guid audiobookId)
    {
        var any = false;
        foreach (var entry in _statusCache)
        {
            if (entry.Key.BookId != audiobookId) continue;
            if (entry.Value != TranscriptStatus.Completed) return false;
            any = true;
        }

        return any;
    }

    /// <summary>
    ///     Deletes a book's transcript without touching the audiobook. A chapter of that
    ///     book currently being transcribed yields at its next window boundary first.
    ///     Note: if the book is still covered by the automatic scope, it will simply be
    ///     re-transcribed — turn the scope down first to keep it transcript-free.
    /// </summary>
    public async Task DeleteTranscriptsAsync(Guid audiobookId)
    {
        _deleteTargetBook = audiobookId;
        try
        {
            // wait (bounded) for a running chapter of this book to yield
            for (var i = 0; i < 100 && _runningChapter?.BookId == audiobookId; i++)
                await Task.Delay(150);

            await _repository.Transcripts.DeleteForAudiobookAsync(audiobookId);
            foreach (var key in _statusCache.Keys.Where(k => k.BookId == audiobookId).ToList())
                _statusCache.TryRemove(key, out _);
        }
        finally
        {
            _deleteTargetBook = null;
        }
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

    private void OnPlayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(PlayerViewModel.NowPlaying)) return;

        ObserveNowPlaying(App.PlayerViewModel.NowPlaying);
        _playbackChanged = true;
        KickWorker();
    }

    private void ObserveNowPlaying(AudiobookViewModel? book)
    {
        if (ReferenceEquals(_observedBook, book)) return;

        if (_observedBook != null) _observedBook.PropertyChanged -= OnNowPlayingPropertyChanged;
        _observedBook = book;
        if (book != null) book.PropertyChanged += OnNowPlayingPropertyChanged;
    }

    private void OnNowPlayingPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(AudiobookViewModel.CurrentChapterIndex))) return;

        _playbackChanged = true;
        KickWorker();
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
                var pick = await PickNextChapterAsync();
                if (pick == null) break;

                _playbackChanged = false;
                await TranscribeChapterAsync(pick.Book, pick.Chapter, cancellationToken);
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
            SetActivity("Idle");
            ScheduleIdleUnload();
        }
    }

    private sealed record ChapterPick(Audiobook Book, ChapterInfo Chapter);

    private async Task<ChapterPick?> PickNextChapterAsync()
    {
        var scope = UserSettings.TranscriptionScope;

        // ①–③ the playing book, in listening order
        if (scope >= 1)
        {
            var playing = App.PlayerViewModel.NowPlaying;
            if (playing != null)
            {
                var pick = await PickFromBookAsync(playing.Model.Id, playing.CurrentChapterIndex ?? 0,
                    retryFailed: false);
                if (pick != null) return pick;
            }
        }

        // ④ manual requests, in request order
        while (_manualQueue.TryPeek(out var manualId))
        {
            var pick = await PickFromBookAsync(manualId, 0, retryFailed: true);
            if (pick != null) return pick;
            _manualQueue.TryDequeue(out _);
        }

        if (scope < 2) return null;

        // ⑤ unfinished books, most recently played first — ⑥ then finished books
        var books = (await _repository.Audiobooks.GetAsync()).ToList();
        foreach (var book in books
                     .OrderBy(b => b.IsCompleted)
                     .ThenByDescending(b => b.DateLastPlayed ?? DateTime.MinValue))
        {
            var pick = await PickFromBookAsync(book.Id, book.CurrentChapterIndex ?? 0, retryFailed: false, book);
            if (pick != null) return pick;
        }

        return null;
    }

    /// <summary>
    ///     First chapter needing work, starting at <paramref name="startChapterIndex" /> and
    ///     wrapping around to the beginning.
    /// </summary>
    private async Task<ChapterPick?> PickFromBookAsync(Guid bookId, int startChapterIndex, bool retryFailed,
        Audiobook? loaded = null)
    {
        if (_deleteTargetBook is Guid deleteTarget && deleteTarget == bookId) return null;

        var needsAnything = _statusCache.IsEmpty ||
                            !_statusCache.Keys.Any(k => k.BookId == bookId) ||
                            _statusCache.Any(kv => kv.Key.BookId == bookId && NeedsWork(kv.Value, retryFailed));
        if (!needsAnything) return null;

        var book = loaded ?? await _repository.Audiobooks.GetAsync(bookId);
        if (book == null || book.Chapters.Count == 0) return null;

        await _repository.Transcripts.EnsureStatusRowsAsync(book.Id,
            book.Chapters.Select(c => (c.Index, c.ParentSourceFileIndex)));
        var statuses = await _repository.Transcripts.GetStatusesAsync(book.Id);
        foreach (var status in statuses)
            _statusCache[(book.Id, status.ChapterIndex)] = status.Status;
        var byIndex = statuses.ToDictionary(s => s.ChapterIndex, s => s.Status);

        var ordered = book.Chapters.OrderBy(c => c.Index).ToList();
        foreach (var chapter in ordered.Where(c => c.Index >= startChapterIndex)
                     .Concat(ordered.Where(c => c.Index < startChapterIndex)))
        {
            if (!byIndex.TryGetValue(chapter.Index, out var status)) status = TranscriptStatus.NotStarted;
            if (NeedsWork(status, retryFailed)) return new ChapterPick(book, chapter);
        }

        return null;
    }

    private static bool NeedsWork(TranscriptStatus status, bool retryFailed)
    {
        return status switch
        {
            TranscriptStatus.Completed => false,
            TranscriptStatus.Failed => retryFailed,
            _ => true
        };
    }

    /// <summary>
    ///     Checked between windows: yield when playback moved and the running chapter is no
    ///     longer the top pick (the chapter being listened to is never preempted by
    ///     movement within itself).
    /// </summary>
    private bool ShouldPreemptCurrentChapter()
    {
        if (!CanTranscribe) return true;
        if (_deleteTargetBook is Guid deleteTarget && _runningChapter?.BookId == deleteTarget) return true;
        if (!_playbackChanged || _runningChapter is not { } running) return false;

        var playing = App.PlayerViewModel.NowPlaying;
        if (playing == null) return false;

        var target = (BookId: playing.Model.Id, ChapterIndex: playing.CurrentChapterIndex ?? 0);
        if (target == running) return false;

        // only preempt when the listener's chapter actually needs transcribing
        return !_statusCache.TryGetValue(target, out var status) ||
               NeedsWork(status, retryFailed: false);
    }

    /// <summary>
    ///     Runs one chapter; failures are persisted and notified (once per book).
    /// </summary>
    private async Task TranscribeChapterAsync(Audiobook book, ChapterInfo chapter,
        CancellationToken cancellationToken)
    {
        var modelId = _backend.Model.Id;
        _runningChapter = (book.Id, chapter.Index);

        void PersistStatus(TranscriptStatus status)
        {
            _statusCache[(book.Id, chapter.Index)] = status;
        }

        var sourceFile = book.SourcePaths.FirstOrDefault(s => s.Index == chapter.ParentSourceFileIndex);
        if (sourceFile == null)
        {
            await _repository.Transcripts.UpdateStatusAsync(book.Id, chapter.Index, TranscriptStatus.Failed, 0,
                $"No source file with index {chapter.ParentSourceFileIndex}.", modelId);
            PersistStatus(TranscriptStatus.Failed);
            StatusChanged?.Invoke(book.Id, chapter.Index, TranscriptStatus.Failed, 0);
            _runningChapter = null;
            return;
        }

        SetActivity($"Loading speech model…");
        await _backend.EnsureLoadedAsync(_modelService.ModelDirectory, cancellationToken);

        // resume an interrupted chapter from its last persisted sentence, unless the
        // existing rows came from a different model (then redo from scratch)
        var previousStatus = (await _repository.Transcripts.GetStatusesAsync(book.Id))
            .FirstOrDefault(s => s.ChapterIndex == chapter.Index);
        var resumeFromMs = 0L;
        if (previousStatus != null && (previousStatus.ModelId.Length == 0 || previousStatus.ModelId == modelId))
            resumeFromMs = await _repository.Transcripts.GetChapterTranscribedUntilAsync(book.Id, chapter.Index);
        if (resumeFromMs == 0)
            await _repository.Transcripts.DeleteChapterSegmentsAsync(book.Id, chapter.Index);

        await _repository.Transcripts.UpdateStatusAsync(book.Id, chapter.Index, TranscriptStatus.InProgress,
            previousStatus?.ProgressPercent ?? 0, null, modelId);
        PersistStatus(TranscriptStatus.InProgress);
        StatusChanged?.Invoke(book.Id, chapter.Index, TranscriptStatus.InProgress, previousStatus?.ProgressPercent ?? 0);
        SetActivity($"Transcribing \"{book.Title}\" — chapter {chapter.Index + 1} of {book.Chapters.Count}");

        var job = new ChapterTranscriptionJob(book.Id, chapter.Index, chapter.ParentSourceFileIndex,
            sourceFile.FilePath, chapter.StartTime, chapter.EndTime, sourceFile.Duration * 1000, modelId,
            resumeFromMs);

        try
        {
            await _transcriber.RunAsync(job,
                pct =>
                {
                    StatusChanged?.Invoke(book.Id, chapter.Index, TranscriptStatus.InProgress, pct);
                    SetActivity(
                        $"Transcribing \"{book.Title}\" — chapter {chapter.Index + 1} of {book.Chapters.Count} ({pct}%)");
                },
                segments => SegmentsFlushed?.Invoke(book.Id, chapter.Index, segments),
                ShouldPreemptCurrentChapter,
                cancellationToken);

            PersistStatus(TranscriptStatus.Completed);
            StatusChanged?.Invoke(book.Id, chapter.Index, TranscriptStatus.Completed, 100);

            if (_statusCache.Where(kv => kv.Key.BookId == book.Id).All(kv => kv.Value == TranscriptStatus.Completed))
                App.ViewModel.EnqueueNotification(new Notification
                {
                    Message = $"Finished transcribing \"{book.Title}\".",
                    Severity = InfoBarSeverity.Success
                });
        }
        catch (Exception e) when (e is OperationCanceledException or TranscriptionPreemptedException)
        {
            // yield cleanly: keep the flushed sentences (the next run resumes after them)
            await _repository.Transcripts.UpdateStatusAsync(book.Id, chapter.Index, TranscriptStatus.Queued, 0,
                null, modelId);
            PersistStatus(TranscriptStatus.Queued);
            StatusChanged?.Invoke(book.Id, chapter.Index, TranscriptStatus.Queued, 0);

            if (e is OperationCanceledException) throw;
        }
        catch (Exception e)
        {
            App.ViewModel.LoggingService.LogError(e, true);
            await _repository.Transcripts.DeleteChapterSegmentsAsync(book.Id, chapter.Index);
            await _repository.Transcripts.UpdateStatusAsync(book.Id, chapter.Index, TranscriptStatus.Failed, 0,
                e.Message, modelId);
            PersistStatus(TranscriptStatus.Failed);
            StatusChanged?.Invoke(book.Id, chapter.Index, TranscriptStatus.Failed, 0);

            if (_failureNotifiedBooks.Add(book.Id))
                App.ViewModel.EnqueueNotification(new Notification
                {
                    Message = $"Transcription of \"{book.Title}\" failed: {e.Message}",
                    Severity = InfoBarSeverity.Warning
                });
        }
        finally
        {
            _runningChapter = null;
        }
    }

    private void SetActivity(string description)
    {
        if (ActivityDescription == description) return;
        ActivityDescription = description;
        ActivityChanged?.Invoke();
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
