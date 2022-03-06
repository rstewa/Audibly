using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Storage;
using Windows.Storage.Streams;
using ATL;
using Audibly.ViewModel;
using FlyleafLib.MediaFramework.MediaDemuxer;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.ObjectModel;

namespace Audibly.Model;

public class Audiobook : BindableBase
{
    public long Duration { get; private set; }
    public string FilePath { get; private set; }

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

    private LinkedList<Demuxer.Chapter> _chptrs = new();
    public LinkedList<Demuxer.Chapter> Chptrs
    {
        get => _chptrs;
        set => SetProperty(ref _chptrs, value);
    }

    private LinkedListNode<Demuxer.Chapter> _curChptr;
    public LinkedListNode<Demuxer.Chapter> CurChptr
    {
        get => _curChptr;
        set
        {
            _curChptr = value;

            CurChptrTitle = _curChptr.Value.Title;
            CurChptrDur = (int)(_curChptr.Value.EndTime - _curChptr.Value.StartTime);
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

    private int _curTimeMs;
    public int CurTimeMs
    {
        get => _curTimeMs;
        set
        {
            SetProperty(ref _curTimeMs, value);
            var t = TimeSpan.FromMilliseconds(_curTimeMs);
            CurTimeText = $@"{(int)t.TotalHours}:{t:mm}:{t:ss}";
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

    // BUG: CurTimeMs is getting updated after skip to next chapter before play
    public void Update(long curMs)
    {
        //if (curMs >= CurChptr.Value.EndTime)
        //{
        //    CurChptr = CurChptr.Next ?? CurChptr;
        //}
        //else if (curMs < CurChptr.Value.StartTime)
        //{
        //    CurChptr = CurChptr.Previous ?? CurChptr;
        //}

        CurChptr = curMs >= CurChptr.Value.EndTime 
            ? CurChptr.Next : curMs < CurChptr.Value.StartTime 
            ? CurChptr.Previous : CurChptr;

        CurTimeMs = (int)(curMs - CurChptr.Value.StartTime);
    }

    public int GetNextChapter()
    {
        // CurTimeMs = 0;
        // CurChptr = CurChptr.Next ?? CurChptr;
        Update((CurChptr.Next ?? CurChptr).Value.StartTime);
        return (int) CurChptr.Value.StartTime;
    }

    public int GetPrevChapter()
    {
        CurTimeMs = 0;

        // will return you the beginning of the current chapter unless you are 2 seconds from the
        // current chapter start time, in which case it will return you the previous chapter
        CurChptr = CurChptr.Previous == null || CurTimeMs - CurChptr.Value.StartTime >= 2000
            ? CurChptr
            : CurChptr.Previous;

        return (int) CurChptr.Value.StartTime;
    }

    public ObservableCollection<Demuxer.Chapter> ChptrList = new();

    public async void Init(string filePath)
    {
        FilePath = filePath;
        var fileMetadata = new Track(filePath);

        Title = fileMetadata.Title;
        Author = fileMetadata.Artist;
        Description = fileMetadata.Comment;
        Duration = fileMetadata.Duration;

        foreach (var ch in fileMetadata.Chapters)
        {
            var chptr = new Demuxer.Chapter
            {
                Title = ch.Title,
                StartTime = ch.StartTime,
                EndTime = ch.EndTime
            };
            Chptrs.AddLast(chptr);
            ChptrList.Add(chptr);

            Debug.WriteLine($"[{chptr.Title}][{chptr.StartTime}][{chptr.EndTime}]");
        }

        CurChptr = Chptrs.First;
        CurTimeMs = 0;

        var imgBytes = fileMetadata.EmbeddedPictures.FirstOrDefault()!.PictureData;
        var imageFile = await StorageFolder.CreateFileAsync("CoverImage.jpg", CreationCollisionOption.ReplaceExisting);
        await FileIO.WriteBytesAsync(imageFile, imgBytes);
        if (imageFile == null) return;

        using var fileStream = await imageFile.OpenAsync(FileAccessMode.Read);
        var bitmapImage = new BitmapImage { DecodePixelWidth = 500 };
        await bitmapImage.SetSourceAsync(fileStream);
        CoverImgSrc = bitmapImage;
    }

    private static StorageFolder StorageFolder => ApplicationData.Current.LocalFolder;
}

public class AudiobookViewModel
{
    public Audiobook Audiobook { get; } = new();
}