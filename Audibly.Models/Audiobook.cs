// Author: rstewa · https://github.com/rstewa
// Updated: 03/17/2025

namespace Audibly.Models;

/// <summary>
///     Represents an audiobook.
/// </summary>
public class Audiobook : DbObject, IEquatable<Audiobook>
{
    public string Author { get; set; }
    public string Composer { get; set; }
    public int CurrentSourceFileIndex { get; set; }
    public DateTime? DateLastPlayed { get; set; }
    public string Description { get; set; }
    public long Duration { get; set; } // *

    // public int CurrentTimeMs { get; set; } // *
    public string CoverImagePath { get; set; }

    public string ThumbnailPath { get; set; }

    public List<SourceFile> SourcePaths { get; set; }
    public bool IsNowPlaying { get; set; }
    public double PlaybackSpeed { get; set; }
    public double Progress { get; set; }
    public Guid? ParentId { get; set; } // Used for grouping
    public DateTime? ReleaseDate { get; set; }
    public string Title { get; set; }
    public double Volume { get; set; }
    public int? CurrentChapterIndex { get; set; }
    public bool IsCompleted { get; set; }

    public List<ChapterInfo> Chapters { get; set; } = [];

    #region IEquatable<Audiobook> Members

    public bool Equals(Audiobook? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Author, other.Author, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(Title, other.Title, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((Audiobook)obj);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Author, StringComparer.OrdinalIgnoreCase);
        hashCode.Add(Title, StringComparer.OrdinalIgnoreCase);
        return hashCode.ToHashCode();
    }
}