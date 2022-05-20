using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Storage;
using ATL;
using Audibly.Extensions;
using FlyleafLib.MediaFramework.MediaDemuxer;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Audibly.Model;

public class Audiobook : BindableBase
{
    private string _author;

    private List<Demuxer.Chapter> _chptrs = new();

    private ImageSource _coverImgSrc = new BitmapImage(new Uri("https://via.placeholder.com/500"));

    private Demuxer.Chapter _curChptr;

    private int _curChptrDur;

    private string _curChptrDurText = "00:00:00";

    private string _curChptrTitle;

    private string _curPosInBook;

    private long _curTimeMs;

    private string _curTimeText = "00:00:00";

    private string _description;

    private string _title;
    public long Duration { get; private set; }
    public string FilePath { get; private set; }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string Author
    {
        get => _author;
        set => SetProperty(ref _author, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public List<Demuxer.Chapter> Chptrs
    {
        get => _chptrs;
        set => SetProperty(ref _chptrs, value);
    }

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

    public string CurChptrTitle
    {
        get => _curChptrTitle;
        set => SetProperty(ref _curChptrTitle, value);
    }

    public int CurChptrDur
    {
        get => _curChptrDur;
        set => SetProperty(ref _curChptrDur, value);
    }

    public string CurChptrDurText
    {
        get => _curChptrDurText;
        set => SetProperty(ref _curChptrDurText, value);
    }

    public int CurTimeMs
    {
        get => (int)_curTimeMs;
        set
        {
            SetProperty(ref _curTimeMs, value);
            CurTimeText = _curTimeMs.ToStr_ms();
        }
    }

    public string CurTimeText
    {
        get => _curTimeText;
        set => SetProperty(ref _curTimeText, value);
    }

    public ImageSource CoverImgSrc
    {
        get => _coverImgSrc;
        set => SetProperty(ref _coverImgSrc, value);
    }

    public string CurPosInBook
    {
        get => $"Current position: {_curPosInBook}";
        set => SetProperty(ref _curPosInBook, value);
    }

    private static StorageFolder StorageFolder => ApplicationData.Current.LocalFolder;

    public void Update(long curMs)
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

    public long GetNextChapter()
    {
        var idx = Chptrs.IndexOf(CurChptr);

        // if 'CurChptr' is the last chapter of the book
        if (idx == Chptrs.Count - 1)
        {
            CurChptr = Chptrs[idx];
            CurTimeMs = (int)CurChptr.EndTime;
            CurPosInBook = CurChptr.EndTime.ToStr_ms();
            return CurChptr.EndTime.ToTicks();
        }

        CurChptr = Chptrs[idx + 1];
        CurTimeMs = 0;
        CurPosInBook = CurChptr.StartTime.ToStr_ms();
        return CurChptr.StartTime.ToTicks();
    }

    public long GetPrevChapter(long curTimeMs)
    {
        var idx = Chptrs.FindIndex(c => c.InRange(curTimeMs));
        if (idx == -1) return curTimeMs.ToTicks();

        // RETURNS start of the current chapter IF 'curTimeMs' is in the 1st chapter of the book
        //     OR
        // the current position in 'CurChptr' is greater than 2 seconds away from the start of 'CurChptr'
        CurChptr = idx == 0 || (curTimeMs > Chptrs[idx].StartTime && curTimeMs - Chptrs[idx].StartTime > 2000)
            ? Chptrs[idx]
            : Chptrs[idx - 1];

        CurTimeMs = 0;
        CurPosInBook = CurChptr.StartTime.ToStr_ms();
        return CurChptr.StartTime.ToTicks();
    }

    public async void Init(string filePath)
    {
        FilePath = filePath;
        var fileMetadata = new Track(filePath);

        Title = fileMetadata.Title;
        Author = fileMetadata.Artist;
        Description = fileMetadata.Comment;
        Duration = fileMetadata.Duration * 1000; // convert to ms

        foreach (var ch in fileMetadata.Chapters)
        {
            var chptr = new Demuxer.Chapter
            {
                Title = ch.Title,
                StartTime = ch.StartTime,
                EndTime = ch.EndTime
            };
            Chptrs.Add(chptr);

#if DEBUG
            Debug.WriteLine($"[{chptr.Title}][{chptr.StartTime}][{chptr.EndTime}]");
#endif
        }

        CurChptr = Chptrs[0];
        CurTimeMs = 0;
        CurPosInBook = ((long)0).ToStr_ms();

        var imgBytes = fileMetadata.EmbeddedPictures.FirstOrDefault()!.PictureData;
        var imageFile = await StorageFolder.CreateFileAsync("CoverImage.jpg", CreationCollisionOption.ReplaceExisting);
        await FileIO.WriteBytesAsync(imageFile, imgBytes);
        if (imageFile == null) return;

        using var fileStream = await imageFile.OpenAsync(FileAccessMode.Read);
        var bitmapImage = new BitmapImage { DecodePixelWidth = 500 };
        await bitmapImage.SetSourceAsync(fileStream);
        CoverImgSrc = bitmapImage;
    }
}

public class AudiobookViewModel
{
    public Audiobook Audiobook { get; } = new();
}