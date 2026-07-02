using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Audibly.App.Helpers;
using Audibly.App.Services.Transcription;
using Audibly.Models;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Audibly.App.ViewModels;

/// <summary>
///     One sentence row of the read-along pane.
/// </summary>
public class TranscriptSegmentViewModel : BindableBase
{
    private static SolidColorBrush? _activeBrush;
    private static readonly SolidColorBrush InactiveBrush = new(Microsoft.UI.Colors.Transparent);

    private bool _isActive;
    private WordTiming[]? _words;

    public TranscriptSegmentViewModel(TranscriptSegment segment)
    {
        Segment = segment;
    }

    public TranscriptSegment Segment { get; }

    public string Text => Segment.Text;

    public int StartMs => Segment.StartMs;

    public int EndMs => Segment.EndMs;

    public Thickness ParagraphMargin => new(0, Segment.IsParagraphStart ? 14 : 2, 0, 2);

    /// <summary>
    ///     Word timings decoded on demand (only the active sentence ever needs them).
    /// </summary>
    public WordTiming[] Words => _words ??= WordTimingCodec.Decode(Segment.WordTimings);

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (!Set(ref _isActive, value)) return;
            OnPropertyChanged(nameof(ActiveBrush));
            OnPropertyChanged(nameof(RowOpacity));
        }
    }

    public Brush ActiveBrush => _isActive ? GetAccentBrush() : InactiveBrush;

    public double RowOpacity => _isActive ? 1.0 : 0.85;

    private static SolidColorBrush GetAccentBrush()
    {
        if (_activeBrush != null) return _activeBrush;

        var accent = (Windows.UI.Color)Application.Current.Resources["SystemAccentColor"];
        _activeBrush = new SolidColorBrush(accent) { Opacity = 0.3 };
        return _activeBrush;
    }
}

/// <summary>
///     Drives the read-along pane: loads the playing chapter's sentences, tracks the
///     active sentence/word from playback position (amortized O(1) per tick), appends
///     live rows while the chapter is still being transcribed, and provides tap-to-seek.
/// </summary>
public class TranscriptViewModel : BindableBase
{
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    private AudiobookViewModel? _observedBook;
    private Guid _displayedBookId;
    private int _displayedChapterIndex = -1;
    private int _activeIndex = -1;
    private int _activeWordIndex = -1;
    private bool _followPlayback = true;
    private bool _isAutoScrollSuspended;
    private string _chapterTitle = "";
    private string _statusText = "";
    private string _placeholderText = "";
    private bool _showPlaceholder;
    private bool _showPendingTail;
    private string _pendingTailText = "Transcribing…";
    private TranscriptStatus _chapterStatus = TranscriptStatus.NotStarted;

    public TranscriptViewModel()
    {
        App.PlayerViewModel.PropertyChanged += OnPlayerPropertyChanged;
        if (App.Transcription != null)
        {
            App.Transcription.StatusChanged += (bookId, chapterIndex, status, pct) =>
                _dispatcherQueue.TryEnqueue(() => OnTranscriptionStatusChanged(bookId, chapterIndex, status, pct));
            App.Transcription.SegmentsFlushed += (bookId, chapterIndex, segments) =>
                _dispatcherQueue.TryEnqueue(() => OnSegmentsFlushed(bookId, chapterIndex, segments));
        }

        ObserveNowPlaying(App.PlayerViewModel.NowPlaying);
        _ = ReloadAsync();
    }

    /// <summary>
    ///     (sentenceIndex, charOffset, charLength) of the active word — UI thread.
    /// </summary>
    public event Action<int, int, int>? ActiveWordChanged;

    /// <summary>
    ///     Index of the newly active sentence — UI thread.
    /// </summary>
    public event Action<int>? ActiveSentenceChanged;

    /// <summary>
    ///     Explicit scroll request (search-hit navigation) — UI thread.
    /// </summary>
    public event Action<int>? ScrollToRequested;

    public ObservableCollection<TranscriptSegmentViewModel> Sentences { get; } = [];

    public bool IsPaneOpen
    {
        get => UserSettings.IsTranscriptPaneOpen;
        set
        {
            UserSettings.IsTranscriptPaneOpen = value;
            OnPropertyChanged();
        }
    }

    public string ChapterTitle
    {
        get => _chapterTitle;
        private set => Set(ref _chapterTitle, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (Set(ref _statusText, value)) OnPropertyChanged(nameof(HasStatusText));
        }
    }

    public bool HasStatusText => _statusText.Length > 0;

    public bool ShowPlaceholder
    {
        get => _showPlaceholder;
        private set => Set(ref _showPlaceholder, value);
    }

    public string PlaceholderText
    {
        get => _placeholderText;
        private set => Set(ref _placeholderText, value);
    }

    public bool ShowPendingTail
    {
        get => _showPendingTail;
        private set => Set(ref _showPendingTail, value);
    }

    public string PendingTailText
    {
        get => _pendingTailText;
        private set => Set(ref _pendingTailText, value);
    }

    /// <summary>
    ///     False after the user scrolled away; the "return to position" chip resumes it.
    /// </summary>
    public bool IsAutoScrollSuspended
    {
        get => _isAutoScrollSuspended;
        set => Set(ref _isAutoScrollSuspended, value);
    }

    public bool FollowPlayback
    {
        get => _followPlayback;
        set => Set(ref _followPlayback, value);
    }

    public void TogglePane()
    {
        IsPaneOpen = !IsPaneOpen;
    }

    #region search

    private readonly List<TranscriptSegment> _searchHits = [];
    private int _currentHitIndex = -1;
    private string _searchQuery = "";
    private DispatcherQueueTimer? _searchDebounce;

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (!Set(ref _searchQuery, value)) return;

            if (_searchDebounce == null)
            {
                _searchDebounce = _dispatcherQueue.CreateTimer();
                _searchDebounce.Interval = TimeSpan.FromMilliseconds(300);
                _searchDebounce.IsRepeating = false;
                _searchDebounce.Tick += (_, _) => _ = RunSearchAsync();
            }

            _searchDebounce.Stop();
            _searchDebounce.Start();
        }
    }

    public string HitCounterText => _searchHits.Count == 0 ? "" : $"{_currentHitIndex + 1}/{_searchHits.Count}";

    public bool HasHits => _searchHits.Count > 0;

    private async Task RunSearchAsync()
    {
        var query = _searchQuery.Trim();
        var bookId = _displayedBookId;

        _searchHits.Clear();
        _currentHitIndex = -1;

        if (query.Length >= 2 && bookId != Guid.Empty)
            try
            {
                var hits = await Task.Run(async () =>
                    await App.Repository.Transcripts.SearchAsync(bookId, query));
                if (query == _searchQuery.Trim() && bookId == _displayedBookId)
                    _searchHits.AddRange(hits);
            }
            catch (Exception e)
            {
                App.ViewModel.LoggingService.LogError(e, false);
            }

        OnPropertyChanged(nameof(HitCounterText));
        OnPropertyChanged(nameof(HasHits));
    }

    public Task GoToNextHitAsync()
    {
        return GoToHitAsync(+1);
    }

    public Task GoToPreviousHitAsync()
    {
        return GoToHitAsync(-1);
    }

    private async Task GoToHitAsync(int direction)
    {
        if (_searchHits.Count == 0) return;

        _currentHitIndex = ((_currentHitIndex + direction) % _searchHits.Count + _searchHits.Count) %
                           _searchHits.Count;
        OnPropertyChanged(nameof(HitCounterText));

        var hit = _searchHits[_currentHitIndex];

        if (hit.ChapterIndex != _displayedChapterIndex)
        {
            // browsing away from the playing chapter — stop following until the chip is used
            FollowPlayback = false;
            IsAutoScrollSuspended = true;
            var title = App.PlayerViewModel.NowPlaying?.Chapters
                .FirstOrDefault(c => c.Index == hit.ChapterIndex)?.Title ?? $"Chapter {hit.ChapterIndex + 1}";
            await LoadChapterAsync(hit.AudiobookId, hit.ChapterIndex, title);
        }

        var index = Sentences.ToList().FindIndex(s => s.Segment.Id == hit.Id);
        if (index < 0)
            index = Sentences.ToList().FindIndex(s => s.StartMs == hit.StartMs);
        if (index >= 0) ScrollToRequested?.Invoke(index);
    }

    public void ClearSearch()
    {
        _searchQuery = "";
        _searchHits.Clear();
        _currentHitIndex = -1;
        OnPropertyChanged(nameof(SearchQuery));
        OnPropertyChanged(nameof(HitCounterText));
        OnPropertyChanged(nameof(HasHits));
        ResumeFollowing();
    }

    #endregion

    /// <summary>
    ///     Tap-to-seek: same source file seeks directly; a different file goes through
    ///     OpenSourceFile (which loads it and seeks).
    /// </summary>
    public async Task SeekToAsync(TranscriptSegmentViewModel sentence)
    {
        var playing = App.PlayerViewModel.NowPlaying;
        if (playing == null) return;

        if (sentence.Segment.SourceFileIndex == playing.CurrentSourceFileIndex)
        {
            App.PlayerViewModel.CurrentPosition = TimeSpan.FromMilliseconds(sentence.StartMs);
            await playing.SaveAsync();
        }
        else
        {
            await App.PlayerViewModel.OpenSourceFile(sentence.Segment.SourceFileIndex,
                sentence.Segment.ChapterIndex, sentence.StartMs);
        }

        ResumeFollowing();
    }

    public void ResumeFollowing()
    {
        FollowPlayback = true;
        IsAutoScrollSuspended = false;

        var playing = App.PlayerViewModel.NowPlaying;
        if (playing != null &&
            (playing.Model.Id != _displayedBookId || (playing.CurrentChapterIndex ?? 0) != _displayedChapterIndex))
        {
            _displayedChapterIndex = -1; // force reload of the playing chapter
            _ = ReloadAsync();
            return;
        }

        if (_activeIndex >= 0) ActiveSentenceChanged?.Invoke(_activeIndex);
    }

    public void RetryChapter()
    {
        if (_displayedBookId != Guid.Empty) App.Transcription?.RequestBook(_displayedBookId);
    }

    private void OnPlayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlayerViewModel.NowPlaying))
        {
            ObserveNowPlaying(App.PlayerViewModel.NowPlaying);
            _ = ReloadAsync();
        }
    }

    private void ObserveNowPlaying(AudiobookViewModel? book)
    {
        if (ReferenceEquals(_observedBook, book)) return;

        if (_observedBook != null) _observedBook.PropertyChanged -= OnNowPlayingPropertyChanged;
        _observedBook = book;
        if (book != null) book.PropertyChanged += OnNowPlayingPropertyChanged;
    }

    private void OnNowPlayingPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(AudiobookViewModel.CurrentChapterIndex):
                _ = ReloadAsync();
                break;
            case nameof(AudiobookViewModel.CurrentTimeMs):
                Advance(App.PlayerViewModel.NowPlaying?.CurrentTimeMs ?? 0);
                break;
        }
    }

    /// <summary>
    ///     Playback-driven reload: shows the playing chapter (unless the user is browsing
    ///     another chapter via search — the return chip brings them back).
    /// </summary>
    private async Task ReloadAsync()
    {
        var playing = App.PlayerViewModel.NowPlaying;
        if (playing == null)
        {
            _displayedBookId = Guid.Empty;
            _displayedChapterIndex = -1;
            Sentences.Clear();
            ChapterTitle = "";
            RefreshPlaceholder();
            return;
        }

        if (!FollowPlayback) return;

        var chapterIndex = playing.CurrentChapterIndex ?? 0;
        await LoadChapterAsync(playing.Model.Id, chapterIndex,
            playing.CurrentChapter?.Title ?? $"Chapter {chapterIndex + 1}");
        Advance(playing.CurrentTimeMs);
    }

    /// <summary>
    ///     Loads one chapter's sentences (off the UI thread) and swaps the list.
    /// </summary>
    private async Task LoadChapterAsync(Guid bookId, int chapterIndex, string title)
    {
        if (bookId == _displayedBookId && chapterIndex == _displayedChapterIndex) return;

        _displayedBookId = bookId;
        _displayedChapterIndex = chapterIndex;
        ChapterTitle = title;

        List<TranscriptSegment> segments = [];
        TranscriptChapterStatus? status = null;
        try
        {
            segments = await Task.Run(async () =>
                await App.Repository.Transcripts.GetSegmentsForChapterAsync(bookId, chapterIndex));
            status = (await Task.Run(async () =>
                    await App.Repository.Transcripts.GetStatusesAsync(bookId)))
                .FirstOrDefault(s => s.ChapterIndex == chapterIndex);
        }
        catch (Exception e)
        {
            App.ViewModel.LoggingService.LogError(e, false);
        }

        // the playing chapter may have changed while we were reading
        if (bookId != _displayedBookId || chapterIndex != _displayedChapterIndex) return;

        Sentences.Clear();
        foreach (var segment in segments) Sentences.Add(new TranscriptSegmentViewModel(segment));

        _activeIndex = -1;
        _activeWordIndex = -1;
        _chapterStatus = status?.Status ?? TranscriptStatus.NotStarted;
        UpdateStatusTexts(status?.ProgressPercent ?? 0, status?.LastError);
        RefreshPlaceholder();
    }

    private void OnTranscriptionStatusChanged(Guid bookId, int chapterIndex, TranscriptStatus status, int pct)
    {
        if (bookId != _displayedBookId || chapterIndex != _displayedChapterIndex) return;

        _chapterStatus = status;
        UpdateStatusTexts(pct, null);
        RefreshPlaceholder();
    }

    private void OnSegmentsFlushed(Guid bookId, int chapterIndex, IReadOnlyList<TranscriptSegment> segments)
    {
        if (bookId != _displayedBookId || chapterIndex != _displayedChapterIndex) return;

        foreach (var segment in segments.OrderBy(s => s.StartMs))
            Sentences.Add(new TranscriptSegmentViewModel(segment));
        RefreshPlaceholder();
    }

    private void UpdateStatusTexts(int pct, string? error)
    {
        StatusText = _chapterStatus switch
        {
            TranscriptStatus.NotStarted => "Not transcribed",
            TranscriptStatus.Queued => "Waiting for transcription…",
            TranscriptStatus.InProgress => $"Transcribing… {pct}%",
            TranscriptStatus.Completed => "",
            TranscriptStatus.Failed => "Transcription failed",
            _ => ""
        };
        PendingTailText = _chapterStatus == TranscriptStatus.InProgress ? $"Transcribing… {pct}%" : "Transcribing…";
    }

    private void RefreshPlaceholder()
    {
        ShowPendingTail = _chapterStatus == TranscriptStatus.InProgress && Sentences.Count > 0;

        if (App.Transcription is { IsSupportedPlatform: false })
            SetPlaceholder("AI transcription is not available on this device.");
        else if (App.PlayerViewModel.NowPlaying == null)
            SetPlaceholder("Nothing is playing.");
        else if (!UserSettings.TranscriptionEnabled)
            SetPlaceholder("AI transcription is turned off.\nEnable it in Settings to get a read-along view.");
        else if (App.TranscriptionModel.State != SpeechModelState.Ready)
            SetPlaceholder("The speech model is not installed.\nDownload it in Settings to get a read-along view.");
        else if (Sentences.Count > 0)
            SetPlaceholder(null);
        else
            SetPlaceholder(_chapterStatus switch
            {
                TranscriptStatus.InProgress => "Transcribing this chapter…",
                TranscriptStatus.Failed => "Transcribing this chapter failed.",
                _ => "This chapter has not been transcribed yet.\nIt is queued and will appear here automatically."
            });
    }

    private void SetPlaceholder(string? text)
    {
        ShowPlaceholder = text != null;
        PlaceholderText = text ?? "";
    }

    /// <summary>
    ///     Playback tick: advance the active sentence cursor (amortized O(1) — forward walk
    ///     for normal playback, binary search after seeks) and the active word within it.
    /// </summary>
    private void Advance(long positionMs)
    {
        if (Sentences.Count == 0) return;

        // only track when the pane is showing the playing chapter
        var playing = App.PlayerViewModel.NowPlaying;
        if (playing == null || playing.Model.Id != _displayedBookId ||
            (playing.CurrentChapterIndex ?? 0) != _displayedChapterIndex) return;

        var index = _activeIndex;

        if (index >= 0 && index < Sentences.Count &&
            positionMs >= Sentences[index].StartMs && positionMs < Sentences[index].EndMs)
        {
            AdvanceWord(positionMs);
            return;
        }

        if (index >= 0 && index + 1 < Sentences.Count && positionMs >= Sentences[index].EndMs)
        {
            // forward walk (normal playback)
            while (index + 1 < Sentences.Count && positionMs >= Sentences[index + 1].StartMs) index++;
            if (positionMs >= Sentences[index].EndMs && positionMs < (index + 1 < Sentences.Count
                    ? Sentences[index + 1].StartMs
                    : int.MaxValue))
            {
                // in a gap between sentences — keep the previous one highlighted
            }
        }
        else
        {
            index = BinarySearch(positionMs);
        }

        if (index < 0 || index >= Sentences.Count) index = Math.Clamp(index, 0, Sentences.Count - 1);
        SetActiveSentence(index, positionMs);
    }

    private int BinarySearch(long positionMs)
    {
        var lo = 0;
        var hi = Sentences.Count - 1;
        while (lo < hi)
        {
            var mid = (lo + hi + 1) / 2;
            if (Sentences[mid].StartMs <= positionMs) lo = mid;
            else hi = mid - 1;
        }

        return lo;
    }

    private void SetActiveSentence(int index, long positionMs)
    {
        if (index != _activeIndex)
        {
            if (_activeIndex >= 0 && _activeIndex < Sentences.Count) Sentences[_activeIndex].IsActive = false;
            _activeIndex = index;
            _activeWordIndex = -1;
            if (_activeIndex >= 0 && _activeIndex < Sentences.Count) Sentences[_activeIndex].IsActive = true;
            ActiveSentenceChanged?.Invoke(_activeIndex);
        }

        AdvanceWord(positionMs);
    }

    private void AdvanceWord(long positionMs)
    {
        if (_activeIndex < 0 || _activeIndex >= Sentences.Count) return;

        var sentence = Sentences[_activeIndex];
        var words = sentence.Words;
        if (words.Length == 0) return;

        var rel = positionMs - sentence.StartMs;
        var wordIndex = -1;
        for (var i = 0; i < words.Length; i++)
        {
            if (words[i].StartMsRel > rel) break;
            wordIndex = i;
        }

        if (wordIndex == _activeWordIndex || wordIndex < 0) return;
        _activeWordIndex = wordIndex;
        ActiveWordChanged?.Invoke(_activeIndex, words[wordIndex].CharOffset, words[wordIndex].CharLength);
    }
}
