namespace Audibly.Models;

/// <summary>
///     One transcribed sentence of an audiobook chapter.
///     Times are absolute milliseconds within the parent source file, the same
///     time domain as <see cref="ChapterInfo.StartTime" /> and playback position.
///     Chapters are referenced by their stable integer index (not by Guid) so
///     transcripts survive re-imports of the same audio files.
/// </summary>
public class TranscriptSegment : DbObject
{
    public Guid AudiobookId { get; set; }
    public Audiobook Audiobook { get; set; }

    /// <summary>
    ///     <see cref="ChapterInfo.Index" /> of the chapter this sentence belongs to.
    /// </summary>
    public int ChapterIndex { get; set; }

    /// <summary>
    ///     <see cref="ChapterInfo.ParentSourceFileIndex" /> of the chapter.
    /// </summary>
    public int SourceFileIndex { get; set; }

    public int StartMs { get; set; }
    public int EndMs { get; set; }

    /// <summary>
    ///     True when a long silence precedes this sentence; the UI renders a paragraph break.
    /// </summary>
    public bool IsParagraphStart { get; set; }

    public string Text { get; set; } = "";

    /// <summary>
    ///     Per-word karaoke timings, packed with <see cref="WordTimingCodec" />.
    /// </summary>
    public byte[] WordTimings { get; set; } = [];
}
