// Author: rstewa · https://github.com/rstewa
// Created: 09/29/2024
// Updated: 10/11/2024

namespace Audibly.Models;

public class SourceFile : DbObject, IEquatable<SourceFile>
{
    public int Index { get; set; }
    public string FilePath { get; set; }
    public int CurrentTimeMs { get; set; }

    public long Duration { get; set; }
    // public int? CurrentChapterIndex { get; set; }
    // public List<ChapterInfo> Chapters { get; set; } = [];

    public bool Equals(SourceFile? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return FilePath == other.FilePath && Index == other.Index;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((SourceFile)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FilePath, Index);
    }
}