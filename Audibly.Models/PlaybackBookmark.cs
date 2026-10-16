namespace Audibly.Models;

public class PlaybackBookmark : DbObject
{
    public Guid AudiobookId { get; set; }
    public int SourceFileIndex { get; set; }
    public int PositionMs { get; set; }
    public string ChapterTitle { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Audiobook Audiobook { get; set; } = null!;
}
