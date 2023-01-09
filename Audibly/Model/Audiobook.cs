//   Author: Ryan Stewart
//   Date: 05/20/2022

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Windows.Storage;
using ATL;
using Audibly.Extensions;
using FlyleafLib.MediaFramework.MediaDemuxer;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Audibly.Model;

public class Audiobook : BindableBase
{
    // CONSTS

    public const string Volume0 = "\uE74F";
    public const string Volume1 = "\uE993";
    public const string Volume2 = "\uE994";
    public const string Volume3 = "\uE995";

    public const string Playback1 = "\uEC57";
    public const string Playback2 = "\uEC58";


    // PROPERTIES
    
    private Metadata metadata;
    public long Duration { get; private set; }
    public string FilePath { get; private set; }

    // TODO:
    private List<Tuple<string, double>> _speeds = new List<Tuple<string, double>>()
    {
        new Tuple<string, double>("0.5x", 0.5),
        new Tuple<string, double>("0.6x", 0.6),
        new Tuple<string, double>("0.7x", 0.7),
        new Tuple<string, double>("0.8x", 0.8),
        new Tuple<string, double>("0.9x", 0.9),
        new Tuple<string, double>("1x", 1),
        new Tuple<string, double>("1.1x", 1.1),
        new Tuple<string, double>("1.2x", 1.2),
        new Tuple<string, double>("1.3x", 1.3),
        new Tuple<string, double>("1.4x", 1.4),
        new Tuple<string, double>("1.5x", 1.5),
        new Tuple<string, double>("1.6x", 1.6),
        new Tuple<string, double>("1.7x", 1.7),
        new Tuple<string, double>("1.8x", 1.8),
        new Tuple<string, double>("1.9x", 1.9),
        new Tuple<string, double>("2x", 2)
    };

    public List<Tuple<string, double>> Speeds
    {
        get { return _speeds; }
    }

    private string _playbackGlyph;
    public string PlaybackGlyph 
    { 
        get => _playbackGlyph;  
        set => SetProperty(ref _playbackGlyph, value);
    }

    private string _audioLevelGlyph;
    public string AudioLevelGlyph 
    { 
        get => _audioLevelGlyph;  
        set => SetProperty(ref _audioLevelGlyph, value);
    }

    private string _title;
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    private string _author;
    public string Author
    {
        get => _author;
        set => SetProperty(ref _author, value);
    }

    private string _description;
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    private List<Demuxer.Chapter> _chptrs = new();
    public List<Demuxer.Chapter> Chptrs
    {
        get => _chptrs ??= new List<Demuxer.Chapter>();
        set => SetProperty(ref _chptrs, value);
    }

    private Demuxer.Chapter _curChptr;
    public Demuxer.Chapter CurChptr
    {
        get => _curChptr;
        set
        {
            _curChptr = value;

            CurChptrTitle = _curChptr.Title;
            CurChptrDur = (int)(_curChptr.EndTime - _curChptr.StartTime);
            // CurChptrDurText = _curChptrDur.ToStr_ms();
            var t = TimeSpan.FromMilliseconds(CurChptrDur);
            CurChptrDurText = $@"{(int)t.TotalHours}:{t:mm}:{t:ss}";
        }
    }

    private string _curChptrTitle;
    public string CurChptrTitle
    {
        get => _curChptrTitle;
        set => SetProperty(ref _curChptrTitle, value);
    }

    private int _curChptrDur;
    public int CurChptrDur
    {
        get => _curChptrDur;
        set => SetProperty(ref _curChptrDur, value);
    }

    private string _curChptrDurText = "00:00:00";
    public string CurChptrDurText
    {
        get => _curChptrDurText;
        set => SetProperty(ref _curChptrDurText, value);
    }

    private long _curTimeMs;
    public int CurTimeMs
    {
        get => (int)_curTimeMs;
        set
        {
            SetProperty(ref _curTimeMs, value);
            CurTimeText = _curTimeMs.ToStr_ms();
        }
    }

    private string _curTimeText = "00:00:00";
    public string CurTimeText
    {
        get => _curTimeText;
        set => SetProperty(ref _curTimeText, value);
    }

    private ImageSource _coverImgSrc = new BitmapImage(new Uri("https://via.placeholder.com/500"));
    public ImageSource CoverImgSrc
    {
        get => _coverImgSrc;
        set => SetProperty(ref _coverImgSrc, value);
    }

    private string _curPosInBook;
    public string CurPosInBook
    {
        get => $"Current position: {_curPosInBook}";
        set => SetProperty(ref _curPosInBook, value);
    }

    private static StorageFolder StorageFolder => ApplicationData.Current.LocalFolder;


    // METHODS

    public void Update(double curMs)
    {
        if (!CurChptr.InRange(curMs))
        {
            var tmp = Chptrs.Find(c => c.InRange(curMs));
            if (tmp != null) CurChptr = tmp;
        }

        CurTimeMs = curMs > CurChptr.StartTime ? (int)(curMs - CurChptr.StartTime) : 0;
        CurPosInBook = curMs.ToStr_ms();
    }

    public long GetChapter(Demuxer.Chapter chptr, long curTimeMs)
    {
        if (chptr == CurChptr) return curTimeMs.ToTicks();
        CurChptr = chptr;
        CurTimeMs = 0;
        CurPosInBook = CurChptr.StartTime.ToStr_ms();
        return CurChptr.StartTime.ToTicks();
    }

    public TimeSpan GetNextChapter()
    {
        var idx = Chptrs.IndexOf(CurChptr);

        // if 'CurChptr' is the last chapter of the book
        if (idx == Chptrs.Count - 1)
        {
            CurChptr = Chptrs[idx];
            CurTimeMs = (int)CurChptr.EndTime;
            CurPosInBook = CurChptr.EndTime.ToStr_ms();
            return TimeSpan.FromMilliseconds(CurChptr.EndTime);
        }

        CurChptr = Chptrs[idx + 1];
        CurTimeMs = 0;
        CurPosInBook = CurChptr.StartTime.ToStr_ms();
        return TimeSpan.FromMilliseconds(CurChptr.StartTime);
    }

    public TimeSpan GetPrevChapter(double curTimeMs)
    {
        var idx = Chptrs.FindIndex(c => c.InRange(curTimeMs));
        if (idx == -1) return TimeSpan.FromMilliseconds(curTimeMs);

        // RETURNS start of the current chapter IF 'curTimeMs' is in the 1st chapter of the book
        //     OR
        // the current position in 'CurChptr' is greater than 3 seconds away from the start of 'CurChptr'
        CurChptr = idx == 0 || (curTimeMs > Chptrs[idx].StartTime && curTimeMs - Chptrs[idx].StartTime > 3000)
            ? Chptrs[idx]
            : Chptrs[idx - 1];

        CurTimeMs = 0;
        CurPosInBook = CurChptr.StartTime.ToStr_ms();
        return TimeSpan.FromMilliseconds(CurChptr.StartTime);
    }

    public void Init(string filePath)
    {
        FilePath = filePath;
        var fileMetadata = new Track(filePath);

        // TESTING
        var json = JsonSerializer.Serialize<Track>(fileMetadata);
        File.WriteAllText($"C:\\Users\\rstewa\\Documents\\AudiblyMetadataTest\\{Path.GetFileNameWithoutExtension(filePath)}_metadata.json", json);

        var bookAppdataDir = StorageFolder.CreateFolderAsync(
            $"{Path.GetFileNameWithoutExtension(filePath)} [{fileMetadata.Artist}]",
            CreationCollisionOption.OpenIfExists).GetAwaiter().GetResult();

        StorageFile coverImage;

        if (!File.Exists(Path.Combine(bookAppdataDir.Path, $"{nameof(Metadata)}.json")))
        {
            metadata = new Metadata
            {
                Title = fileMetadata.Title,
                Author = fileMetadata.Artist,
                Description = fileMetadata.AdditionalFields.ContainsKey("\u00A9des")
                    ? fileMetadata.AdditionalFields["\u00A9des"]
                    : fileMetadata.Comment,
                Duration = fileMetadata.Duration.ToMs(),
                Chapters = new List<Demuxer.Chapter>()
            };

            foreach (var ch in fileMetadata.Chapters)
            {
                var chptr = new Demuxer.Chapter
                {
                    Title = ch.Title,
                    StartTime = ch.StartTime,
                    EndTime = ch.EndTime
                };
                metadata.Chapters.Add(chptr);
            }
             
            var imageBytes = fileMetadata.EmbeddedPictures.FirstOrDefault()?.PictureData;
            coverImage = bookAppdataDir.CreateFileAsync("CoverImage.jpg", CreationCollisionOption.ReplaceExisting)
                .GetAwaiter().GetResult();
            FileIO.WriteBytesAsync(coverImage, imageBytes).GetAwaiter().GetResult();

            var metadataFile = bookAppdataDir.CreateFileAsync("Metadata.json", CreationCollisionOption.ReplaceExisting)
                .GetAwaiter().GetResult();
            File.WriteAllText(metadataFile.Path,
                JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            metadata = JsonSerializer.Deserialize<Metadata>(
                File.ReadAllText(Path.Combine(bookAppdataDir.Path, $"{nameof(Metadata)}.json")));
            coverImage = bookAppdataDir.GetFileAsync("CoverImage.jpg").GetAwaiter().GetResult();
        }

        Debug.Assert(metadata != null, nameof(metadata) + " != null");

        Title = metadata.Title;
        Author = metadata.Author;
        Description = metadata.Description;
        Duration = metadata.Duration;
        Chptrs = metadata.Chapters;

        CurChptr = Chptrs[0];
        CurTimeMs = 0;
        CurPosInBook = "0";
        AudioLevelGlyph = Volume3;
        PlaybackGlyph = Playback1;
        // PlaybackGlyph = Playback2;

        var bitmapImage = new BitmapImage(new Uri(coverImage.Path)) { DecodePixelWidth = 500 };
        CoverImgSrc = bitmapImage;
    }
}

public class AudiobookViewModel
{
    public Audiobook Audiobook { get; } = new();
}