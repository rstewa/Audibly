using ATL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Chapter = FlyleafLib.MediaFramework.MediaDemuxer.Demuxer.Chapter;

namespace Audibly.Models;

// TODO: remake this & create Audiobook view class
public class Audiobook : INotifyPropertyChanged
{
    public Audiobook(string filePath)
    {
        FilePath = filePath;
        var fileMetadata = new Track(filePath);

        Title = fileMetadata.Title;
        Author = fileMetadata.Artist;
        Description = fileMetadata.Comment;
        Duration = fileMetadata.Duration;

        Chapters = new List<Chapter>(fileMetadata.Chapters.Count);
        foreach (var ch in fileMetadata.Chapters)
        {
            var chapter = new Chapter
            {
                Title = ch.Title,
                StartTime = ch.StartTime,
                EndTime = ch.EndTime
            };
            Chapters.Add(chapter);
        }

        CurrentChapter = Chapters[0];
        CurrentTime = "00:00:00";
    }

    public bool IsNextChapter(long curMs)
    {
        return curMs > CurrentChapter.EndTime;
    }

    public void UpdateCurrentChapter(long curMs)
    {
        if (CurrentChapter.InRange(curMs)) return;
        CurrentChapter = Chapters.Find(c => c.InRange(curMs));
    }

    public string FilePath;
    public string Title;
    public string Author;
    public string Description;
    public long Duration;
    public List<Chapter> Chapters;

    private Chapter _currentChapter = null;
    public string CurrentChapterTitle = string.Empty;
    public Chapter CurrentChapter
    {
        get
        {
            return _currentChapter;
        }
        set
        {
            if (value != _currentChapter)
            {
                _currentChapter = value;
                CurrentChapterTitle = _currentChapter.Title;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentChapterTitle)));
            }
        }
    }

    public string CurrentTime = string.Empty;
    private long _currentTimeMs = 0;
    public long CurrentTimeMs
    {
        get { return _currentTimeMs; }
        set
        {
            if (_currentTimeMs != value)
            {
                _currentTimeMs = value;
                var t = TimeSpan.FromMilliseconds(_currentTimeMs);
                CurrentTime = $@"{(int)t.TotalHours}:{t:mm}:{t:ss}";
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTime)));
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
}