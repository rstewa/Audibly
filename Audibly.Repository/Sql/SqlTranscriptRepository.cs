using Audibly.Models;
using Audibly.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Audibly.Repository.Sql;

public class SqlTranscriptRepository(AudiblyContext db) : ITranscriptRepository
{
    public async Task<List<TranscriptSegment>> GetSegmentsForChapterAsync(Guid audiobookId, int chapterIndex)
    {
        return await db.TranscriptSegments
            .Where(s => s.AudiobookId == audiobookId && s.ChapterIndex == chapterIndex)
            .OrderBy(s => s.StartMs)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<TranscriptSegment>> GetAllSegmentsAsync(Guid audiobookId)
    {
        return await db.TranscriptSegments
            .Where(s => s.AudiobookId == audiobookId)
            .OrderBy(s => s.ChapterIndex)
            .ThenBy(s => s.StartMs)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<TranscriptChapterStatus>> GetStatusesAsync(Guid audiobookId)
    {
        return await db.TranscriptChapterStatuses
            .Where(s => s.AudiobookId == audiobookId)
            .OrderBy(s => s.ChapterIndex)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<TranscriptChapterStatus>> GetAllStatusesAsync()
    {
        return await db.TranscriptChapterStatuses
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task EnsureStatusRowsAsync(Guid audiobookId,
        IEnumerable<(int ChapterIndex, int SourceFileIndex)> chapters)
    {
        var existing = await db.TranscriptChapterStatuses
            .Where(s => s.AudiobookId == audiobookId)
            .Select(s => s.ChapterIndex)
            .ToListAsync();
        var existingSet = existing.ToHashSet();

        var missing = chapters
            .Where(c => !existingSet.Contains(c.ChapterIndex))
            .Select(c => new TranscriptChapterStatus
            {
                AudiobookId = audiobookId,
                ChapterIndex = c.ChapterIndex,
                SourceFileIndex = c.SourceFileIndex,
                Status = TranscriptStatus.NotStarted,
                UpdatedAtUtc = DateTime.UtcNow
            })
            .ToList();

        if (missing.Count == 0) return;

        db.TranscriptChapterStatuses.AddRange(missing);
        await db.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(Guid audiobookId, int chapterIndex, TranscriptStatus status,
        int progressPercent, string? error, string modelId)
    {
        await db.TranscriptChapterStatuses
            .Where(s => s.AudiobookId == audiobookId && s.ChapterIndex == chapterIndex)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, status)
                .SetProperty(x => x.ProgressPercent, progressPercent)
                .SetProperty(x => x.LastError, error)
                .SetProperty(x => x.ModelId, modelId)
                .SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow));
    }

    public async Task AddSegmentsAsync(IReadOnlyList<TranscriptSegment> segments)
    {
        if (segments.Count == 0) return;

        db.TranscriptSegments.AddRange(segments);
        await db.SaveChangesAsync();
    }

    public async Task CompleteChapterAsync(Guid audiobookId, int chapterIndex,
        IReadOnlyList<TranscriptSegment> segments, string modelId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        if (segments.Count > 0)
        {
            db.TranscriptSegments.AddRange(segments);
            await db.SaveChangesAsync();
        }

        await db.TranscriptChapterStatuses
            .Where(s => s.AudiobookId == audiobookId && s.ChapterIndex == chapterIndex)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, TranscriptStatus.Completed)
                .SetProperty(x => x.ProgressPercent, 100)
                .SetProperty(x => x.LastError, (string?)null)
                .SetProperty(x => x.ModelId, modelId)
                .SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow));

        await transaction.CommitAsync();
    }

    public async Task DeleteChapterSegmentsAsync(Guid audiobookId, int chapterIndex)
    {
        await db.TranscriptSegments
            .Where(s => s.AudiobookId == audiobookId && s.ChapterIndex == chapterIndex)
            .ExecuteDeleteAsync();
    }

    public async Task<int> GetChapterTranscribedUntilAsync(Guid audiobookId, int chapterIndex)
    {
        return await db.TranscriptSegments
            .Where(s => s.AudiobookId == audiobookId && s.ChapterIndex == chapterIndex)
            .MaxAsync(s => (int?)s.EndMs) ?? 0;
    }

    public async Task DeleteForAudiobookAsync(Guid audiobookId)
    {
        await db.TranscriptSegments
            .Where(s => s.AudiobookId == audiobookId)
            .ExecuteDeleteAsync();
        await db.TranscriptChapterStatuses
            .Where(s => s.AudiobookId == audiobookId)
            .ExecuteDeleteAsync();
    }

    public async Task<List<TranscriptSegment>> SearchAsync(Guid audiobookId, string query, int limit = 200)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        var escaped = query
            .Replace(@"\", @"\\")
            .Replace("%", @"\%")
            .Replace("_", @"\_");

        return await db.TranscriptSegments
            .Where(s => s.AudiobookId == audiobookId &&
                        EF.Functions.Like(s.Text, $"%{escaped}%", @"\"))
            .OrderBy(s => s.ChapterIndex)
            .ThenBy(s => s.StartMs)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> ResetInterruptedAsync()
    {
        // keep any partial segments — the worker resumes from the last flushed sentence
        return await db.TranscriptChapterStatuses
            .Where(s => s.Status == TranscriptStatus.InProgress)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, TranscriptStatus.Queued)
                .SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow));
    }
}
