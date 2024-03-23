// Author: rstewa · https://github.com/rstewa
// Created: 2/23/2024
// Updated: 3/16/2024

namespace Audibly.Models;

/// <summary>
///     Represents an audiobook.
/// </summary>
public class Audiobook : DbObject, IEquatable<Audiobook>
{
    public string Author { get; set; }
    public string Composer { get; set; }
    public string Description { get; set; }
    public long Duration { get; set; }
    public int CurrentTimeMs { get; set; }

    public string CoverImagePath { get; set; }

    // public string CurrentPositionInBook { get; set; }
    public string FilePath { get; set; }
    public bool IsNowPlaying { get; set; }
    public double PlaybackSpeed { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public string Title { get; set; }
    public double Volume { get; set; }
    // public string VolumeLevelGlyph { get; set; }

    // public ChapterInfo? CurrentChapter { get; set; }
    public int? CurrentChapterIndex { get; set; }

    public List<ChapterInfo> Chapters { get; init; } = [];

    public bool Equals(Audiobook? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Author, other.Author, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(Title, other.Title, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Audiobook)obj);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Author, StringComparer.OrdinalIgnoreCase);
        hashCode.Add(Title, StringComparer.OrdinalIgnoreCase);
        return hashCode.ToHashCode();
    }
}