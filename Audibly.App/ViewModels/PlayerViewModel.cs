// Author: rstewa · https://github.com/rstewa
// Created: 3/5/2024
// Updated: 3/16/2024

using System.Threading.Tasks;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;

namespace Audibly.App.ViewModels;

public class PlayerViewModel : BindableBase
{
    private readonly DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    private AudiobookViewModel _nowPlaying;

    public AudiobookViewModel NowPlaying
    {
        get => _nowPlaying;
        set => Set(ref _nowPlaying, value);
    }

    public async Task GetChapterListAsync()
    {
        await dispatcherQueue.EnqueueAsync(() => { NowPlaying.Chapters.Clear(); });
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
}