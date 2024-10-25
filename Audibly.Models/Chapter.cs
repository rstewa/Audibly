// Author: rstewa · https://github.com/rstewa
// Created: 04/15/2024
// Updated: 10/11/2024

namespace Audibly.Models;

/// <summary>
///     Information describing a chapter
/// </summary>
public class ChapterInfo : DbObject, IEquatable<ChapterInfo>
{
    public int ParentSourceFileIndex { get; set; }

    /// <summary>
    ///     This is a sequential value that is used to keep the Chapters in order
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    ///     Start time (ms)
    ///     NB : Only used when <see cref="UseOffset" /> is false
    /// </summary>
    public uint StartTime { get; set; }

    /// <summary>
    ///     End time (ms)
    ///     NB : Only used when <see cref="UseOffset" /> is false
    /// </summary>
    public uint EndTime { get; set; }

    /// <summary>
    ///     Start offset (bytes)
    ///     NB1 : Only used when <see cref="UseOffset" /> is true
    ///     NB2 : Only supported by ID3v2
    /// </summary>
    public uint StartOffset { get; set; }

    /// <summary>
    ///     End offset (bytes)
    ///     NB1 : Only used when <see cref="UseOffset" /> is true
    ///     NB2 : Only supported by ID3v2
    /// </summary>
    public uint EndOffset { get; set; }

    /// <summary>
    ///     True to use StartOffset / EndOffset instead of StartTime / EndTime
    ///     NB : Only supported by ID3v2
    ///     Default : false
    /// </summary>
    public bool UseOffset { get; set; }

    /// <summary>
    ///     Title
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    ///     Unique ID
    ///     ID3v2 : Unique ID
    ///     Vorbis : Chapter index (0,1,2...)
    /// </summary>
    public string UniqueID { get; set; }

    /// <summary>
    ///     Subtitle
    ///     NB : Only supported by ID3v2
    /// </summary>
    public string Subtitle { get; set; }
    
    public Guid AudiobookId { get; set; }
    public Audiobook Audiobook { get; set; }

    // ---------------- CONSTRUCTORS

    /// <summary>
    ///     Construct an empty structure
    /// </summary>
    public ChapterInfo(uint startTime = 0, string title = "")
    {
        StartTime = startTime;
        EndTime = 0;
        StartOffset = 0;
        EndOffset = 0;
        UseOffset = false;
        Title = title;
        UniqueID = "";
        Subtitle = "";
    }

    /// <summary>
    ///     Construct a structure by copying information from the given ChapterInfo
    /// </summary>
    /// <param name="chapter">Structure to copy information from</param>
    public ChapterInfo(ChapterInfo chapter)
    {
        StartTime = chapter.StartTime;
        EndTime = chapter.EndTime;
        StartOffset = chapter.StartOffset;
        EndOffset = chapter.EndOffset;
        Title = chapter.Title;
        Subtitle = chapter.Subtitle;
        UniqueID = chapter.UniqueID;
    }

    // public bool Equals(Audiobook? other)
    // {
    //     if (ReferenceEquals(null, other)) return false;
    //     if (ReferenceEquals(this, other)) return true;
    //     return string.Equals(Title, other.Title, StringComparison.OrdinalIgnoreCase);
    // }

    public bool Equals(ChapterInfo? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Title, other.Title, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ChapterInfo);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(ParentSourceFileIndex);
        hashCode.Add(Index);
        hashCode.Add(StartTime);
        hashCode.Add(EndTime);
        hashCode.Add(StartOffset);
        hashCode.Add(EndOffset);
        hashCode.Add(UseOffset);
        hashCode.Add(Title);
        hashCode.Add(UniqueID);
        hashCode.Add(Subtitle);
        return hashCode.ToHashCode();
    }
}