using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Audibly.Models;
using Audibly.Repository.Interfaces;

namespace Audibly.App.Services.Transcription;

/// <summary>
///     Thrown between windows when the scheduler wants the running chapter to yield to a
///     higher-priority one. Not an error: the chapter's partial segments are purged and it
///     is re-queued.
/// </summary>
public class TranscriptionPreemptedException : Exception;

/// <summary>
///     Everything needed to transcribe one chapter. Times are absolute ms within the
///     source file, the same domain as playback and <see cref="ChapterInfo" />.
/// </summary>
public record ChapterTranscriptionJob(
    Guid AudiobookId,
    int ChapterIndex,
    int SourceFileIndex,
    string FilePath,
    long ChapterStartMs,
    long ChapterEndMs,
    long FileDurationMs,
    string ModelId);

/// <summary>
///     Transcribes one chapter: decodes it in ≤30-minute blocks, runs 40 s windows with
///     5 s overlap through the speech backend, stitches the overlaps, groups words into
///     sentences and flushes them incrementally so the UI can show text while the chapter
///     is still being transcribed. The final flush marks the chapter Completed atomically.
/// </summary>
public class ChapterTranscriber(
    IAudiblyRepository repository,
    ISpeechToTextBackend backend,
    IPcmAudioExtractor extractor)
{
    public const int WindowMs = 40_000;
    public const int OverlapMs = 5_000;
    public const int StrideMs = WindowMs - OverlapMs;
    private const int BlockMs = 30 * 60_000;
    private const int MinTailWindowMs = 2_000;
    private const int FlushBatchSize = 20;

    public async Task RunAsync(ChapterTranscriptionJob job, Action<int> onProgress,
        Action<IReadOnlyList<TranscriptSegment>> onFlushed, Func<bool> shouldPreempt,
        CancellationToken cancellationToken)
    {
        var startMs = Math.Clamp(job.ChapterStartMs, 0, job.FileDurationMs);
        var endMs = Math.Clamp(job.ChapterEndMs, 0, job.FileDurationMs);
        if (endMs <= startMs)
            throw new PcmExtractionException(
                $"Invalid chapter bounds: [{job.ChapterStartMs}, {job.ChapterEndMs}] in a {job.FileDurationMs} ms file.");

        var windows = BuildWindows(startMs, endMs);
        var assembler = new SentenceAssembler();
        var carry = new List<TimedWord>();
        var batch = new List<TranscriptSegment>();

        IPcmAudioSource? source = null;
        long blockStartMs = 0;
        long blockEndMs = 0;

        try
        {
            for (var i = 0; i < windows.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (shouldPreempt()) throw new TranscriptionPreemptedException();

                var (winStart, winEnd) = windows[i];

                if (source == null || winEnd > blockEndMs)
                {
                    if (source != null) await source.DisposeAsync();
                    source = null;
                    blockStartMs = winStart;
                    blockEndMs = Math.Min(blockStartMs + BlockMs, endMs);
                    source = await extractor.OpenRangeAsync(job.FilePath, blockStartMs, blockEndMs - blockStartMs,
                        cancellationToken);
                }

                var pcm = await source.ReadWindowAsync(winStart - blockStartMs, winEnd - winStart, cancellationToken);
                var words = pcm.Length == 0 ? Array.Empty<TimedWord>() : backend.TranscribeWindow(pcm, winStart);

                var overlapEnd = i == 0 ? winStart : Math.Min(windows[i - 1].EndMs, winEnd);
                var merged = OverlapStitcher.Merge(carry, words, winStart, overlapEnd);

                // words the next window can no longer affect are final
                var finalBoundary = i == windows.Count - 1 ? long.MaxValue : windows[i + 1].StartMs;
                var finalCount = 0;
                while (finalCount < merged.Count && merged[finalCount].StartMs < finalBoundary) finalCount++;

                batch.AddRange(assembler.Push(merged.Take(finalCount)).Select(d => ToSegment(job, d)));
                carry.Clear();
                carry.AddRange(merged.Skip(finalCount));

                var progress = Math.Min(99, (i + 1) * 100 / windows.Count);
                if (batch.Count >= FlushBatchSize)
                {
                    await FlushAsync(job, batch, progress, onFlushed);
                }
                else
                {
                    await repository.Transcripts.UpdateStatusAsync(job.AudiobookId, job.ChapterIndex,
                        TranscriptStatus.InProgress, progress, null, job.ModelId);
                }

                onProgress(progress);
            }

            batch.AddRange(assembler.Push(carry).Select(d => ToSegment(job, d)));
            if (assembler.Flush() is { } trailing) batch.Add(ToSegment(job, trailing));

            await repository.Transcripts.CompleteChapterAsync(job.AudiobookId, job.ChapterIndex, batch, job.ModelId);
            if (batch.Count > 0) onFlushed(batch);
            onProgress(100);
        }
        finally
        {
            if (source != null) await source.DisposeAsync();
        }
    }

    private async Task FlushAsync(ChapterTranscriptionJob job, List<TranscriptSegment> batch, int progress,
        Action<IReadOnlyList<TranscriptSegment>> onFlushed)
    {
        var flushed = batch.ToList();
        await repository.Transcripts.AddSegmentsAsync(flushed);
        await repository.Transcripts.UpdateStatusAsync(job.AudiobookId, job.ChapterIndex,
            TranscriptStatus.InProgress, progress, null, job.ModelId);
        batch.Clear();
        onFlushed(flushed);
    }

    private static TranscriptSegment ToSegment(ChapterTranscriptionJob job, SentenceDraft draft)
    {
        return new TranscriptSegment
        {
            AudiobookId = job.AudiobookId,
            ChapterIndex = job.ChapterIndex,
            SourceFileIndex = job.SourceFileIndex,
            StartMs = draft.StartMs,
            EndMs = draft.EndMs,
            IsParagraphStart = draft.IsParagraphStart,
            Text = draft.Text,
            WordTimings = WordTimingCodec.Encode(draft.Words)
        };
    }

    private static List<(long StartMs, long EndMs)> BuildWindows(long startMs, long endMs)
    {
        var windows = new List<(long, long)>();
        for (var ws = startMs; ws < endMs; ws += StrideMs)
            windows.Add((ws, Math.Min(ws + WindowMs, endMs)));

        // a tiny trailing window transcribes badly — extend the previous window instead
        if (windows.Count > 1 && windows[^1].Item2 - windows[^1].Item1 < MinTailWindowMs)
        {
            windows.RemoveAt(windows.Count - 1);
            windows[^1] = (windows[^1].Item1, endMs);
        }

        return windows;
    }
}
