using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.ApplicationModel.DataTransfer;
using Audibly.App.ViewModels;
using Audibly.ReadAlong;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;

namespace Audibly.App.UserControls;

public sealed partial class ReadAlongPanel : UserControl
{
    private readonly ReadAlongStore _store = new(ApplicationData.Current.LocalFolder.Path);
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(50) };
    private CancellationTokenSource? _operation;
    private AlignmentMap? _map;
    private BookAssociation? _association;
    private Guid? _book;
    private Passage? _lastPassage;
    private Cue? _lastCue;
    private bool _follow = true;
    private bool _busy;
    private bool _loading = true;
    private readonly Stopwatch _processingClock = new();
    private long _lastElapsedSecond = -1;
    private string? _lastError;
    private string? _lastDiagnosticPath;
    private string? _lastErrorContext;
    private ScrollViewer? _scroll;
    private static string EngineFolder => Path.Combine(AppContext.BaseDirectory, "read-along");
    private AudiobookViewModel? Book => App.PlayerViewModel.NowPlaying;
    public ReadAlongPanel()
    {
        InitializeComponent();
        Loaded += OnLoaded; Unloaded += OnUnloaded;
        _timer.Tick += (_, _) => { UpdatePosition(); UpdateElapsed(); };
        Passages.AddHandler(PointerWheelChangedEvent, new PointerEventHandler((_, _) => StopFollowing()), true);
        Passages.AddHandler(PointerPressedEvent, new PointerEventHandler((_, _) => StopFollowing()), true);
        Passages.AddHandler(KeyDownEvent, new KeyEventHandler((_, _) => StopFollowing()), true);
    }
    private void StopFollowing()
    {
        if (_map is null) return;
        _follow = false; FollowButton.Visibility = Visibility.Visible;
        ModeLabel.Text = "Scrolling freely"; ModeLabel.Visibility = Visibility.Visible;
    }
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        App.PlayerViewModel.PropertyChanged += PlayerChanged;
        _scroll = FindChild<ScrollViewer>(Passages);
        if (_scroll is not null) _scroll.DirectManipulationStarted += Scroll_ManipulationStarted;
        _timer.Start(); await LoadBookAsync();
    }
    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        App.PlayerViewModel.PropertyChanged -= PlayerChanged;
        if (_scroll is not null) _scroll.DirectManipulationStarted -= Scroll_ManipulationStarted;
        _timer.Stop(); _operation?.Cancel();
    }
    private void Scroll_ManipulationStarted(object? sender, object e) => StopFollowing();
    private async void PlayerChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlayerViewModel.NowPlaying)) await LoadBookAsync();
    }
    private static string[] SourcePaths(AudiobookViewModel book) => book.SourcePaths.Select(s => s.FilePath).ToArray();
    private void SetBusy(bool busy)
    {
        _busy = busy; AttachButton.IsEnabled = GenerateButton.IsEnabled = ReprepareButton.IsEnabled = AudioLanguage.IsEnabled = ProcessingDevice.IsEnabled = !busy;
        CancelButton.IsEnabled = busy;
        ProcessingPanel.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        if (busy)
        {
            _processingClock.Restart(); _lastElapsedSecond = -1;
            ProcessingProgress.IsIndeterminate = true;
            ProcessingStage.Text = "Preparing your ebook";
            ProcessingDetail.Text = "Checking your files";
        }
        else _processingClock.Stop();
        UpdateSetup();
    }
    private void UpdateSetup()
    {
        AttachButton.Visibility = _association is null ? Visibility.Collapsed : Visibility.Visible;
        ReprepareButton.Visibility = _map is null ? Visibility.Collapsed : Visibility.Visible;
        EpubLabel.Text = _association is null ? "Read alongside your audiobook" : _association.EpubName ?? "Your ebook";
        ToolTipService.SetToolTip(EpubLabel, EpubLabel.Text);
        GenerateButton.Content = _association is null ? "Add ebook" : "Set up Read Along";
        EmptyTitle.Text = _association is null ? "Read as you listen" : "Follow every word";
        EmptyDescription.Text = _association is null ? "Add this book's EPUB to follow the narration."
            : "Set up word highlighting for this audiobook. You only need to do this once.";
        LoadingPanel.Visibility = _loading ? Visibility.Visible : Visibility.Collapsed;
        EmptyPanel.Visibility = !_loading && !_busy && _map is null ? Visibility.Visible : Visibility.Collapsed;
        Passages.Visibility = _map is not null ? Visibility.Visible : Visibility.Collapsed;
        FollowButton.Visibility = !_follow && _map is not null ? Visibility.Visible : Visibility.Collapsed;
        ModeLabel.Visibility = !_follow && _map is not null ? Visibility.Visible : Visibility.Collapsed;
    }
    private void UpdateElapsed()
    {
        if (!_busy || (long)_processingClock.Elapsed.TotalSeconds == _lastElapsedSecond) return;
        _lastElapsedSecond = (long)_processingClock.Elapsed.TotalSeconds;
        ProcessingElapsed.Text = "Elapsed " + _processingClock.Elapsed.ToString(@"hh\:mm\:ss");
    }
    private void ReportProgress(string line)
    {
        if (AlignmentProgress.TryParse(line, out var progress) && progress is not null)
        {
            ProcessingStage.Text = progress.Stage switch
            {
                "Checking ebook and audio files" => "Checking your files",
                "Preparing ebook text" => "Opening your ebook",
                "Loading speech model" => "Getting ready",
                "Matching words to audio" => "Matching text to narration",
                "Saving and checking alignment" or "Verifying saved alignment" => "Finishing up",
                "Read Along is ready" => "Your ebook is ready",
                _ => progress.Stage
            };
            ProcessingProgress.IsIndeterminate = progress.Fraction is null;
            if (progress.Fraction is { } fraction) ProcessingProgress.Value = fraction * 100;
            ProcessingDetail.Text = progress.Detail?.Contains("fallback", StringComparison.OrdinalIgnoreCase) == true ? "Using CPU. Graphics processing isn't available."
                : progress.Detail?.Contains("first use", StringComparison.OrdinalIgnoreCase) == true ? "Downloading what we need for your first ebook."
                : progress.Detail?.Contains("GPU", StringComparison.OrdinalIgnoreCase) == true ? "Using graphics processing"
                : progress.Detail?.Contains("CPU", StringComparison.OrdinalIgnoreCase) == true ? "Using CPU"
                : progress.Detail ?? "";
        }
    }
    private void ShowFailure(string message, string? diagnosticPath = null, string? context = null)
    {
        _lastError = message; _lastDiagnosticPath = diagnosticPath; _lastErrorContext = context;
        StatusText.Text = message;
        ErrorNotice.IsOpen = true;
        CopyErrorButton.Content = "Copy error details"; CopyErrorButton.Visibility = Visibility.Visible;
    }
    private void ClearFailure()
    {
        _lastError = null; _lastDiagnosticPath = null; _lastErrorContext = null;
        CopyErrorButton.Visibility = Visibility.Collapsed;
        ErrorNotice.IsOpen = false;
    }
    private async void CopyError_Click(object sender, RoutedEventArgs e)
    {
        if (_lastError is not { } error) return;
        try
        {
            var text = await FailureReport.CreateAsync(error, _lastDiagnosticPath, _lastErrorContext);
            var package = new DataPackage(); package.SetText(text); Clipboard.SetContent(package);
            CopyErrorButton.Content = "Copied";
        }
        catch (Exception ex) { CopyErrorButton.Content = "Copy failed"; StatusText.Text = error + "\n" + ex.Message; }
    }
    private async Task LoadBookAsync()
    {
        _operation?.Cancel();
        var operation = _operation = new CancellationTokenSource(); var token = operation.Token;
        _book = Book?.Id; _map = null; _association = null; _lastPassage = null; _lastCue = null; _loading = true;
        ClearFailure();
        Passages.ItemsSource = null; SetBusy(false); _follow = true;
        if (Book is not { } book) { _loading = false; UpdateSetup(); EmptyDescription.Text = "Open an audiobook to get started."; GenerateButton.IsEnabled = false; return; }
        try
        {
            StatusText.Text = "Checking saved Read Along data…";
            var association = await _store.LoadAsync(book.Id, token);
            token.ThrowIfCancellationRequested();
            if (_operation != operation) return;
            if (association is null) { StatusText.Text = "Attach the matching EPUB to this audiobook."; return; }
            var shared = App.PlayerViewModel.ReadAlongDocument;
            var sources = SourcePaths(book);
            var map = shared is not null && shared.AudiobookId == book.Id &&
                string.Equals(shared.Epub.Path, association.EpubPath, StringComparison.OrdinalIgnoreCase) &&
                shared.Sources.Select(s => s.Path).SequenceEqual(sources, StringComparer.OrdinalIgnoreCase)
                ? shared : await Task.Run(async () =>
                {
                    var saved = await _store.LoadMapAsync(book.Id, association, sources, token);
                    return saved is null ? null : await ReadingLayout.CreateAsync(saved, token);
                }, token);
            token.ThrowIfCancellationRequested();
            if (_operation != operation) return;
            _association = association; _map = map;
            App.PlayerViewModel.ReadAlongDocument = map;
            UpdateSetup();
            Passages.ItemsSource = map?.Passages;
            StatusText.Text = map is not null ? "Ready · published EPUB text · click an aligned word to seek"
                : association.Error?.Contains("only supports EPUB 3", StringComparison.Ordinal) == true
                    ? "Your ebook needs an update. Choose Set up Read Along to try again."
                    : association.Error ?? "";
            if (map is null && association.Error is { } savedError)
            {
                var diagnosticPath = association.DiagnosticPath;
                if (diagnosticPath is null && savedError.LastIndexOf("Details: ", StringComparison.Ordinal) is var index && index >= 0)
                    diagnosticPath = savedError[(index + 9)..].Trim();
                if (diagnosticPath is not null && !Path.GetFullPath(diagnosticPath).StartsWith(_store.BookFolder(book.Id) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    diagnosticPath = null;
                ShowFailure(StatusText.Text, diagnosticPath);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            if (_operation == operation) ShowFailure(e.Message + " Attach an EPUB or generate alignment again.", context: e.ToString());
            // Keep association available for regeneration when only the map is stale.
            if (_operation == operation) try { _association = await _store.LoadAsync(book.Id); } catch { }
            if (_operation == operation) UpdateSetup();
        }
        finally { if (_operation == operation) { _loading = false; UpdateSetup(); } }
    }
    private async void Attach_Click(object sender, RoutedEventArgs e)
    {
        BookSettingsFlyout.Hide();
        await AttachEpubAsync();
    }
    private async Task<bool> AttachEpubAsync()
    {
        if (Book is not { } book || _busy) return false;
        try
        {
            var picker = new FileOpenPicker(); picker.FileTypeFilter.Add(".epub");
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.Window));
            var file = await picker.PickSingleFileAsync();
            if (file is null || Book?.Id != book.Id) return false;
            _operation?.Cancel(); SetBusy(true);
            await _store.AttachAsync(book.Id, file.Path);
            await LoadBookAsync();
            return Book?.Id == book.Id && _association is not null;
        }
        catch (Exception ex) { ShowFailure(ex.Message, context: ex.ToString()); }
        finally { SetBusy(false); }
        return false;
    }
    private async void Generate_Click(object sender, RoutedEventArgs e)
    {
        if (_busy || Book is not { } book) return;
        BookSettingsFlyout.Hide();
        if (_association is null && !await AttachEpubAsync()) return;
        if (Book?.Id != book.Id || _association is not { } association) return;
        _operation?.Cancel(); var operation = _operation = new CancellationTokenSource(); var token = operation.Token;
        ClearFailure();
        SetBusy(true); var sources = SourcePaths(book); var language = AudioLanguage.Text.Trim();
        var device = (ProcessingDevice.SelectedItem as ComboBoxItem)?.Tag as string ?? "dml";
        try
        {
            StatusText.Text = "Preparing this book for Read Along…";
            var progress = new Progress<string>(text => { if (_operation == operation) ReportProgress(text); });
            var path = await new StorytellerProcessor().GenerateAsync(EngineFolder, Path.Combine(_store.BookFolder(book.Id), "cache"),
                book.Id, association.EpubPath, sources, language, progress, token, device);
            ReportProgress("PROGRESS:{\"stage\":\"Verifying saved alignment\"}");
            var updated = new BookAssociation { EpubPath = association.EpubPath, EpubName = association.EpubName, MapPath = path, Status = "ready" };
            // Validate result before replacing the last good association.
            var map = await Task.Run(async () =>
            {
                var saved = await _store.LoadMapAsync(book.Id, updated, sources, token);
                return saved is null ? null : await ReadingLayout.CreateAsync(saved, token);
            }, token);
            token.ThrowIfCancellationRequested();
            await _store.SaveAsync(book.Id, updated, token);
            if (_operation != operation || Book?.Id != book.Id) return;
            _association = updated; _map = map; Passages.ItemsSource = map?.Passages;
            App.PlayerViewModel.ReadAlongDocument = map;
            _follow = true; _lastPassage = null; _lastCue = null;
            StatusText.Text = "Ready · reliable word timing · gaps remain unhighlighted";
        }
        catch (OperationCanceledException) { if (_operation == operation) StatusText.Text = "Setup cancelled. You can continue later."; }
        catch (Exception ex)
        {
            if (_operation == operation)
            {
                var diagnosticPath = (ex as AlignmentProcessingException)?.DiagnosticPath;
                ShowFailure(ex.Message, diagnosticPath, "Stage: " + ProcessingStage.Text + "\n" + ex);
                association.Status = "failed"; association.Error = ex.Message;
                association.DiagnosticPath = diagnosticPath;
                try { await _store.SaveAsync(book.Id, association); } catch { }
            }
        }
        finally { if (_operation == operation) SetBusy(false); }
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) => _operation?.Cancel();
    private void Follow_Click(object sender, RoutedEventArgs e)
    {
        _follow = true; _lastPassage = null;
        FollowButton.Visibility = ModeLabel.Visibility = Visibility.Collapsed; UpdatePosition();
    }
    private void UpdatePosition()
    {
        if (Visibility != Visibility.Visible || _map is null || Book is not { } book || book.Id != _book) return;
        var current = _map.Find(book.CurrentSourceFileIndex, App.PlayerViewModel.CurrentPosition.TotalSeconds);
        var cue = current?.Cue;
        if (_follow && current is { } active && active.Passage != _lastPassage)
        {
            Passages.ScrollIntoView(active.Passage, ScrollIntoViewAlignment.Leading);
            _lastPassage = active.Passage;
        }
        if (_lastCue == cue) return;
        _lastCue = cue;
        foreach (var row in VisibleRows())
        {
            row.Highlight(cue);
            if (_follow) row.BringWordIntoView(cue);
        }
    }
    private System.Collections.Generic.IEnumerable<ReadAlongPassage> VisibleRows()
    {
        var panel = Passages.ItemsPanelRoot;
        if (panel is null) yield break;
        foreach (var child in panel.Children)
            if (FindChild<ReadAlongPassage>(child) is { } row) yield return row;
    }
    private void Passages_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (!args.InRecycleQueue) args.RegisterUpdateCallback((_, a) =>
        {
            if (FindChild<ReadAlongPassage>(a.ItemContainer) is not { } row) return;
            row.Highlight(_lastCue);
            if (_follow) row.BringWordIntoView(_lastCue);
        });
    }
    private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent is T value) return value;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            if (FindChild<T>(VisualTreeHelper.GetChild(parent, i)) is { } child) return child;
        return null;
    }
}
