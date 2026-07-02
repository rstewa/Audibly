using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Audibly.Models;

namespace Audibly.App.Services.Transcription;

/// <summary>
///     Converts between persisted transcript rows and the portable .audibly export shape.
/// </summary>
public static class TranscriptExportMapper
{
    /// <summary>
    ///     Builds the export payload for one audiobook; null when it has no finished chapters.
    /// </summary>
    public static async Task<TranscriptExport?> BuildAsync(Guid audiobookId)
    {
        var statuses = (await App.Repository.Transcripts.GetStatusesAsync(audiobookId))
            .Where(s => s.Status is TranscriptStatus.Completed or TranscriptStatus.Failed)
            .ToList();
        if (statuses.Count == 0) return null;

        var segmentsByChapter = (await App.Repository.Transcripts.GetAllSegmentsAsync(audiobookId))
            .GroupBy(s => s.ChapterIndex)
            .ToDictionary(g => g.Key, g => g.ToList());

        var export = new TranscriptExport
        {
            ModelId = statuses.FirstOrDefault(s => s.ModelId.Length > 0)?.ModelId ?? ""
        };

        foreach (var status in statuses.OrderBy(s => s.ChapterIndex))
        {
            var chapter = new TranscriptChapterExport
            {
                ChapterIndex = status.ChapterIndex,
                SourceFileIndex = status.SourceFileIndex,
                Status = (int)status.Status
            };

            if (segmentsByChapter.TryGetValue(status.ChapterIndex, out var segments))
                chapter.Segments.AddRange(segments.Select(ToExport));

            export.Chapters.Add(chapter);
        }

        return export;
    }

    /// <summary>
    ///     Inserts an imported transcript for a freshly upserted audiobook, validating each
    ///     chapter against the re-parsed book. Re-import is idempotent (existing rows of the
    ///     same chapter are replaced).
    /// </summary>
    public static async Task ImportAsync(Audiobook audiobook, TranscriptExport transcript)
    {
        await App.Repository.Transcripts.EnsureStatusRowsAsync(audiobook.Id,
            audiobook.Chapters.Select(c => (c.Index, c.ParentSourceFileIndex)));

        foreach (var chapter in transcript.Chapters)
        {
            var known = audiobook.Chapters.FirstOrDefault(c => c.Index == chapter.ChapterIndex);
            if (known == null || known.ParentSourceFileIndex != chapter.SourceFileIndex ||
                chapter.SourceFileIndex >= audiobook.SourcePaths.Count)
            {
                App.ViewModel.LoggingService.Log(
                    $"Transcript import: skipping chapter {chapter.ChapterIndex} of \"{audiobook.Title}\" (chapter layout changed).");
                continue;
            }

            await App.Repository.Transcripts.DeleteChapterSegmentsAsync(audiobook.Id, chapter.ChapterIndex);

            var segments = chapter.Segments.Select(s => new TranscriptSegment
            {
                AudiobookId = audiobook.Id,
                ChapterIndex = chapter.ChapterIndex,
                SourceFileIndex = chapter.SourceFileIndex,
                StartMs = s.S,
                EndMs = s.E,
                IsParagraphStart = s.P,
                Text = s.T,
                WordTimings = WordTimingCodec.Encode(DecodeQuads(s.W))
            }).ToList();

            var status = (TranscriptStatus)chapter.Status == TranscriptStatus.Completed
                ? TranscriptStatus.Completed
                : TranscriptStatus.Failed;

            if (status == TranscriptStatus.Completed)
            {
                await App.Repository.Transcripts.CompleteChapterAsync(audiobook.Id, chapter.ChapterIndex,
                    segments, transcript.ModelId);
            }
            else
            {
                await App.Repository.Transcripts.AddSegmentsAsync(segments);
                await App.Repository.Transcripts.UpdateStatusAsync(audiobook.Id, chapter.ChapterIndex, status,
                    0, null, transcript.ModelId);
            }
        }
    }

    private static TranscriptSegmentExport ToExport(TranscriptSegment segment)
    {
        var words = WordTimingCodec.Decode(segment.WordTimings);
        var quads = new int[words.Length * 4];
        for (var i = 0; i < words.Length; i++)
        {
            quads[i * 4] = words[i].CharOffset;
            quads[i * 4 + 1] = words[i].CharLength;
            quads[i * 4 + 2] = words[i].StartMsRel;
            quads[i * 4 + 3] = words[i].DurationMs;
        }

        return new TranscriptSegmentExport
        {
            S = segment.StartMs,
            E = segment.EndMs,
            P = segment.IsParagraphStart,
            T = segment.Text,
            W = quads
        };
    }

    private static List<WordTiming> DecodeQuads(int[] quads)
    {
        var words = new List<WordTiming>(quads.Length / 4);
        for (var i = 0; i + 3 < quads.Length; i += 4)
            words.Add(new WordTiming(
                (ushort)Math.Clamp(quads[i], 0, ushort.MaxValue),
                (ushort)Math.Clamp(quads[i + 1], 0, ushort.MaxValue),
                (ushort)Math.Clamp(quads[i + 2], 0, ushort.MaxValue),
                (ushort)Math.Clamp(quads[i + 3], 0, ushort.MaxValue)));
        return words;
    }
}
