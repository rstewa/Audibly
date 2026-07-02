namespace Audibly.Models;

/// <summary>
///     Transcription progress of one (audiobook, chapter) pair.
/// </summary>
public class TranscriptChapterStatus : DbObject
{
    public Guid AudiobookId { get; set; }
    public Audiobook Audiobook { get; set; }

    /// <summary>
    ///     <see cref="ChapterInfo.Index" /> of the chapter.
    /// </summary>
    public int ChapterIndex { get; set; }

    /// <summary>
    ///     <see cref="ChapterInfo.ParentSourceFileIndex" /> of the chapter.
    /// </summary>
    public int SourceFileIndex { get; set; }

    public TranscriptStatus Status { get; set; }

    /// <summary>
    ///     Window progress within the chapter, 0-100, only meaningful while InProgress.
    /// </summary>
    public int ProgressPercent { get; set; }

    /// <summary>
    ///     Id of the speech model that produced (or is producing) this chapter's transcript.
    /// </summary>
    public string ModelId { get; set; } = "";

    public string? LastError { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
