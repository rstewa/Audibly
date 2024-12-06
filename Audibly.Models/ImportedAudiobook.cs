// Author: rstewa · https://github.com/rstewa
// Created: 11/20/2024
// Updated: 11/20/2024

namespace Audibly.Models;

public class ImportedAudiobook
{
    public int CurrentTimeMs { get; set; }
    public string CoverImagePath { get; set; }
    public string FilePath { get; set; }
    public double Progress { get; set; }
    public int? CurrentChapterIndex { get; set; }
    public bool IsNowPlaying { get; set; }
    public bool IsCompleted { get; set; }
}