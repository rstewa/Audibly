// Author: rstewa · https://github.com/rstewa
// Created: 12/05/2024
// Updated: 12/05/2024

namespace Audibly.Models.v1;

public class Audiobooks
{
    public string Author { get; set; }
    public string Composer { get; set; }
    public DateTime? DateLastPlayed { get; set; }
    public string Description { get; set; }
    public long Duration { get; set; }
    public int CurrentTimeMs { get; set; }
    public string CoverImagePath { get; set; }
    public string ThumbnailPath { get; set; }
    public string FilePath { get; set; }
    public bool IsNowPlaying { get; set; }
    public double PlaybackSpeed { get; set; }
    public double Progress { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public string Title { get; set; }
    public double Volume { get; set; }
    public int? CurrentChapterIndex { get; set; }

    public List<Chapter> Chapters { get; init; } = [];
}