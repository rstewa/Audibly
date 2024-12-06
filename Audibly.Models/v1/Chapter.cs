// Author: rstewa · https://github.com/rstewa
// Created: 12/06/2024
// Updated: 12/06/2024

namespace Audibly.Models.v1;

public class Chapter
{
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
}