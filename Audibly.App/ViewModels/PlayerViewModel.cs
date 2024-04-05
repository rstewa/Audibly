// Author: rstewa · https://github.com/rstewa
// Created: 3/21/2024
// Updated: 3/22/2024

using System.Linq;
using Windows.Media.Playback;
using Audibly.App.Extensions;
using Audibly.App.Helpers;
using Microsoft.UI.Dispatching;

namespace Audibly.App.ViewModels;

public class PlayerViewModel : BindableBase
{
    /// <summary>
    ///     Gets the app-wide MediaPlayer instance.
    /// </summary>
    public readonly MediaPlayer MediaPlayer = new();

    private readonly DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    private AudiobookViewModel? _nowPlaying;

    public AudiobookViewModel? NowPlaying
    {
        get => _nowPlaying;
        set => Set(ref _nowPlaying, value);
    }

    private string _volumeLevelGlyph;

    public string VolumeLevelGlyph
    {
        get => _volumeLevelGlyph;
        set => Set(ref _volumeLevelGlyph, value);
    }

    private double _volumeLevel;

    public double VolumeLevel
    {
        get => _volumeLevel;
        set => Set(ref _volumeLevel, value);
    }

    private string _chapterDurationText = "0:00:00";

    public string ChapterDurationText
    {
        get => _chapterDurationText;
        set => Set(ref _chapterDurationText, value);
    }

    private string _chapterPositionText = "0:00:00";

    public string ChapterPositionText
    {
        get => _chapterPositionText;
        set => Set(ref _chapterPositionText, value);
    }

    public void Update(double curMs)
    {
        if (!NowPlaying.CurrentChapter.InRange(curMs))
        {
            var tmp = NowPlaying.Chapters.FirstOrDefault(c => c.InRange(curMs));
            if (tmp != null) NowPlaying.CurrentChapter = tmp;
        }

        ChapterPositionMs = curMs > NowPlaying.CurrentChapter.StartTime
            ? (int)(curMs - NowPlaying.CurrentChapter.StartTime)
            : 0;
        // ChapterPositionText = ChapterPositionMs.ToStr_ms();
        ChapterPositionText = "11:11:11";
        // CurPosInBook = curMs.ToStr_ms();
    }

    private long _chapterPositionMs;

    public int ChapterPositionMs
    {
        get => (int)_chapterPositionMs;
        set
        {
            Set(ref _chapterPositionMs, value);
            ChapterPositionText = _chapterPositionMs.ToStr_ms();
        }
    }

    private long _chapterDurationMs;

    public int ChapterDurationMs
    {
        get => (int)_chapterDurationMs;
        set
        {
            Set(ref _chapterDurationMs, value);
            ChapterDurationText = _chapterDurationMs.ToStr_ms();
        }
    }

    private bool _fullScreenPlayer = false;

    public bool FullScreenPlayer
    {
        get => _fullScreenPlayer;
        set => Set(ref _fullScreenPlayer, value);
    }

    private string _maximizeMinimizeGlyph = Constants.MaximizeGlyph;
    public string MaximizeMinimizeGlyph
    {
        get => _maximizeMinimizeGlyph;
        set => Set(ref _maximizeMinimizeGlyph, value);
    }
}