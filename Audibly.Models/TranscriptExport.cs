namespace Audibly.Models;

/// <summary>
///     Portable transcript payload embedded (optionally) in the .audibly export JSON.
///     Word timings use flat int quads (charOffset, charLength, startMsRel, durationMs)
///     so the format stays independent of the packed storage codec.
/// </summary>
public class TranscriptExport
{
    public int SchemaVersion { get; set; } = 1;
    public string ModelId { get; set; } = "";
    public List<TranscriptChapterExport> Chapters { get; set; } = [];
}

public class TranscriptChapterExport
{
    public int ChapterIndex { get; set; }
    public int SourceFileIndex { get; set; }

    /// <summary>
    ///     <see cref="TranscriptStatus" /> as int; only Completed/Failed chapters are exported.
    /// </summary>
    public int Status { get; set; }

    public List<TranscriptSegmentExport> Segments { get; set; } = [];
}

public class TranscriptSegmentExport
{
    /// <summary>StartMs.</summary>
    public int S { get; set; }

    /// <summary>EndMs.</summary>
    public int E { get; set; }

    /// <summary>IsParagraphStart.</summary>
    public bool P { get; set; }

    /// <summary>Text.</summary>
    public string T { get; set; } = "";

    /// <summary>Word timings as flat quads.</summary>
    public int[] W { get; set; } = [];
}
