using Audibly.Models;

namespace Audibly.Repository.Interfaces;

public interface ITranscriptRepository
{
    /// <summary>
    ///     Returns the segments of one chapter, ordered by start time.
    /// </summary>
    Task<List<TranscriptSegment>> GetSegmentsForChapterAsync(Guid audiobookId, int chapterIndex);

    /// <summary>
    ///     Returns all segments of an audiobook ordered by chapter and start time (export).
    /// </summary>
    Task<List<TranscriptSegment>> GetAllSegmentsAsync(Guid audiobookId);

    /// <summary>
    ///     Returns the per-chapter statuses of an audiobook.
    /// </summary>
    Task<List<TranscriptChapterStatus>> GetStatusesAsync(Guid audiobookId);

    /// <summary>
    ///     Returns every chapter status in the database (scheduler bootstrap).
    /// </summary>
    Task<List<TranscriptChapterStatus>> GetAllStatusesAsync();

    /// <summary>
    ///     Creates missing status rows (as NotStarted) for the given chapters.
    /// </summary>
    Task EnsureStatusRowsAsync(Guid audiobookId, IEnumerable<(int ChapterIndex, int SourceFileIndex)> chapters);

    /// <summary>
    ///     Targeted update of one chapter's status row; the row must already exist.
    /// </summary>
    Task UpdateStatusAsync(Guid audiobookId, int chapterIndex, TranscriptStatus status,
        int progressPercent, string? error, string modelId);

    /// <summary>
    ///     Bulk-inserts segments (single transaction). Used for incremental flushes.
    /// </summary>
    Task AddSegmentsAsync(IReadOnlyList<TranscriptSegment> segments);

    /// <summary>
    ///     Final flush of a chapter: inserts the remaining segments and marks the chapter
    ///     Completed in one transaction, so Completed never coexists with a missing tail.
    /// </summary>
    Task CompleteChapterAsync(Guid audiobookId, int chapterIndex,
        IReadOnlyList<TranscriptSegment> segments, string modelId);

    /// <summary>
    ///     Deletes all segments of one chapter (preemption/retry cleanup).
    /// </summary>
    Task DeleteChapterSegmentsAsync(Guid audiobookId, int chapterIndex);

    /// <summary>
    ///     Deletes all transcript data (segments and statuses) of an audiobook.
    /// </summary>
    Task DeleteForAudiobookAsync(Guid audiobookId);

    /// <summary>
    ///     Case-insensitive substring search over one audiobook's transcript.
    /// </summary>
    Task<List<TranscriptSegment>> SearchAsync(Guid audiobookId, string query, int limit = 200);

    /// <summary>
    ///     Startup recovery: purges partial segments of chapters left InProgress by a
    ///     crash/shutdown and re-queues them. Returns the number of chapters reset.
    /// </summary>
    Task<int> ResetInterruptedAsync();
}
