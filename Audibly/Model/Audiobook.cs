//   Author: Ryan Stewart
//   Date: 05/20/2022

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
    // TODO: there's for sure a better way to do this ... lol
    public event EventHandler ViewChanged;
    protected virtual void OnViewChanged(EventArgs e)
    {
        ViewChanged?.Invoke(this, e);
    }

    private bool _isCompact;
    public bool IsCompact
    {
        get => _isCompact;
        set
        {
            _isCompact = value;
            OnViewChanged(new ViewChangedEventArgs { IsCompact = value });
        }
    }
    
    // CONSTS
    public const string Volume0 = "\uE74F";
    public const string Volume1 = "\uE993";
    public const string Volume2 = "\uE994";
    public const string Volume3 = "\uE995";

    // PROPERTIES
    private Metadata _metadata;
    private long Duration { get; set; }
    public string FilePath { get; private set; }

    private string _volumeLevelGlyph;
    public string VolumeLevelGlyph 
    { 
        get => _volumeLevelGlyph;  
        set => SetProperty(ref _volumeLevelGlyph, value);
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

    private List<Demuxer.Chapter> _chapters = new();
    public List<Demuxer.Chapter> Chapters
    {
        get => _chapters ??= new List<Demuxer.Chapter>();
        set => SetProperty(ref _chapters, value);
    }

    private Demuxer.Chapter _curChapter;
    public Demuxer.Chapter CurChapter
    {
        get => _curChapter;
        set
        {
            _curChapter = value;

            CurChapterTitle = _curChapter.Title;
            CurChapterDur = (int)(_curChapter.EndTime - _curChapter.StartTime);
            // CurChapterDurText = _curChapterDur.ToStr_ms();
            var t = TimeSpan.FromMilliseconds(CurChapterDur);
            CurChapterDurText = $@"{(int)t.TotalHours}:{t:mm}:{t:ss}";
        }
    }

    private string _curChapterTitle;
    public string CurChapterTitle
    {
        get => _curChapterTitle;
        set => SetProperty(ref _curChapterTitle, value);
    }

    private int _curChapterDur;
    public int CurChapterDur
    {
        get => _curChapterDur;
        set => SetProperty(ref _curChapterDur, value);
    }

    private string _curChapterDurText = "00:00:00";
    public string CurChapterDurText
    {
        get => _curChapterDurText;
        set => SetProperty(ref _curChapterDurText, value);
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

    private ImageSource _coverImgSrc = new BitmapImage(new Uri(
        Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "black_square_500x500.png")));
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

    private double _volume;
    public double Volume
    {
        get => _volume;
        set => SetProperty(ref _volume, value);
    }

    private static StorageFolder StorageFolder => ApplicationData.Current.LocalFolder;


    // METHODS

    public void Update(double curMs)
    {
        if (!CurChapter.InRange(curMs))
        {
            var tmp = Chapters.Find(c => c.InRange(curMs));
            if (tmp != null) CurChapter = tmp;
        }

        CurTimeMs = curMs > CurChapter.StartTime ? (int)(curMs - CurChapter.StartTime) : 0;
        CurPosInBook = curMs.ToStr_ms();
    }

    public TimeSpan GetNextChapter()
    {
        var idx = Chapters.IndexOf(CurChapter);

        // if 'CurChapter' is the last chapter of the book
        if (idx == Chapters.Count - 1)
        {
            CurChapter = Chapters[idx];
            CurTimeMs = (int)CurChapter.EndTime;
            CurPosInBook = CurChapter.EndTime.ToStr_ms();
            return TimeSpan.FromMilliseconds(CurChapter.EndTime);
        }

        CurChapter = Chapters[idx + 1];
        CurTimeMs = 0;
        CurPosInBook = CurChapter.StartTime.ToStr_ms();
        return TimeSpan.FromMilliseconds(CurChapter.StartTime);
    }

    public TimeSpan GetPrevChapter(double curTimeMs)
    {
        var idx = Chapters.FindIndex(c => c.InRange(curTimeMs));
        if (idx == -1) return TimeSpan.FromMilliseconds(curTimeMs);

        // RETURNS start of the current chapter IF 'curTimeMs' is in the 1st chapter of the book
        //     OR
        // the current position in 'CurChapter' is greater than 3 seconds away from the start of 'CurChapter'
        CurChapter = idx == 0 || (curTimeMs > Chapters[idx].StartTime && curTimeMs - Chapters[idx].StartTime > 3000)
            ? Chapters[idx]
            : Chapters[idx - 1];

        CurTimeMs = 0;
        CurPosInBook = CurChapter.StartTime.ToStr_ms();
        return TimeSpan.FromMilliseconds(CurChapter.StartTime);
    }

    public void Init(string filePath)
    {
        FilePath = filePath;
        var fileMetadata = new Track(filePath);

        // TESTING
        // var json = JsonSerializer.Serialize<Track>(fileMetadata);
        // File.WriteAllText($"C:\\Users\\rstewa\\Documents\\AudiblyMetadataTest\\{Path.GetFileNameWithoutExtension(filePath)}_metadata.json", json);

        var bookAppdataDir = StorageFolder.CreateFolderAsync(
            $"{Path.GetFileNameWithoutExtension(filePath)} [{fileMetadata.Artist}]",
            CreationCollisionOption.OpenIfExists).GetAwaiter().GetResult();

        StorageFile coverImage;

        if (!File.Exists(Path.Combine(bookAppdataDir.Path, $"{nameof(Metadata)}.json")))
        {
            _metadata = new Metadata
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
                var chapter = new Demuxer.Chapter
                {
                    Title = ch.Title,
                    StartTime = ch.StartTime,
                    EndTime = ch.EndTime
                };
                _metadata.Chapters.Add(chapter);
            }
             
            var imageBytes = fileMetadata.EmbeddedPictures.FirstOrDefault()?.PictureData;
            coverImage = bookAppdataDir.CreateFileAsync("CoverImage.jpg", CreationCollisionOption.ReplaceExisting)
                .GetAwaiter().GetResult();
            FileIO.WriteBytesAsync(coverImage, imageBytes).GetAwaiter().GetResult();

            var metadataFile = bookAppdataDir.CreateFileAsync("Metadata.json", CreationCollisionOption.ReplaceExisting)
                .GetAwaiter().GetResult();
            File.WriteAllText(metadataFile.Path,
                JsonSerializer.Serialize(_metadata, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            _metadata = JsonSerializer.Deserialize<Metadata>(
                File.ReadAllText(Path.Combine(bookAppdataDir.Path, $"{nameof(Metadata)}.json")));
            coverImage = bookAppdataDir.GetFileAsync("CoverImage.jpg").GetAwaiter().GetResult();
        }

        Debug.Assert(_metadata != null, $"{nameof(Metadata)} cannot be null");

        Title = _metadata.Title;
        Author = _metadata.Author;
        Description = _metadata.Description;
        Duration = _metadata.Duration;
        Chapters = _metadata.Chapters;

        CurChapter = Chapters[0];
        CurTimeMs = 0;
        CurPosInBook = "0";
        
        Volume = 100;
        VolumeLevelGlyph = Volume3;

        var bitmapImage = new BitmapImage(new Uri(coverImage.Path)) { DecodePixelWidth = 500 };
        CoverImgSrc = bitmapImage;
    }
}

public static class AudiobookViewModel
{
    private static Audiobook _audiobook = new();
    public static Audiobook Audiobook { get { return _audiobook; } }
}