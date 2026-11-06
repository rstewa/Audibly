using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
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
    private ScrollViewer? _scroll;
    private string EngineFolder
    {
        get => ApplicationData.Current.LocalSettings.Values["ReadAlongEngine"] as string ?? Path.Combine(AppContext.BaseDirectory, "read-along");
        set => ApplicationData.Current.LocalSettings.Values["ReadAlongEngine"] = value;
    }
    private AudiobookViewModel? Book => App.PlayerViewModel.NowPlaying;
    public ReadAlongPanel()
    {
        InitializeComponent();
        Loaded += OnLoaded; Unloaded += OnUnloaded;
        _timer.Tick += (_, _) => UpdatePosition();
        Passages.AddHandler(PointerWheelChangedEvent, new PointerEventHandler((_, _) => StopFollowing()), true);
        Passages.AddHandler(PointerPressedEvent, new PointerEventHandler((_, _) => StopFollowing()), true);
        Passages.AddHandler(KeyDownEvent, new KeyEventHandler((_, _) => StopFollowing()), true);
    }
    private void StopFollowing() { _follow = false; ModeLabel.Text = "Manual scrolling · choose Follow narration to resume"; }
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
        _busy = busy; AttachButton.IsEnabled = GenerateButton.IsEnabled = EngineButton.IsEnabled = AudioLanguage.IsEnabled = !busy;
        CancelButton.IsEnabled = busy;
    }
    private async Task LoadBookAsync()
    {
        _operation?.Cancel();
        var operation = _operation = new CancellationTokenSource(); var token = operation.Token;
        _book = Book?.Id; _map = null; _association = null; _lastPassage = null; _lastCue = null;
        Passages.ItemsSource = null; SetBusy(false); _follow = true;
        if (Book is not { } book) { StatusText.Text = "Open an audiobook first."; return; }
        try
        {
            StatusText.Text = "Checking saved Read Along data…";
            var association = await _store.LoadAsync(book.Id, token);
            token.ThrowIfCancellationRequested();
            if (_operation != operation) return;
            if (association is null) { StatusText.Text = "Attach the matching EPUB to this audiobook."; return; }
            var map = await Task.Run(() => _store.LoadMapAsync(book.Id, association, SourcePaths(book), token), token);
            token.ThrowIfCancellationRequested();
            if (_operation != operation) return;
            _association = association; _map = map;
            Passages.ItemsSource = map?.Passages;
            StatusText.Text = map is not null ? "Ready · published EPUB text · click an aligned word to seek" : association.Error ?? "EPUB attached. Generate alignment to begin.";
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            if (_operation == operation) StatusText.Text = e.Message + " Attach an EPUB or generate alignment again.";
            // Keep association available for regeneration when only the map is stale.
            if (_operation == operation) try { _association = await _store.LoadAsync(book.Id); } catch { }
        }
    }
    private async void Attach_Click(object sender, RoutedEventArgs e)
    {
        if (Book is not { } book || _busy) return;
        try
        {
            var picker = new FileOpenPicker(); picker.FileTypeFilter.Add(".epub");
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.Window));
            var file = await picker.PickSingleFileAsync();
            if (file is null || Book?.Id != book.Id) return;
            _operation?.Cancel(); SetBusy(true);
            await _store.AttachAsync(book.Id, file.Path);
            await LoadBookAsync();
        }
        catch (Exception ex) { StatusText.Text = ex.Message; }
        finally { SetBusy(false); }
    }
    private async void Engine_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FolderPicker(); picker.FileTypeFilter.Add("*");
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.Window));
            var folder = await picker.PickSingleFolderAsync();
            if (folder is null) return;
            if (!File.Exists(Path.Combine(folder.Path, "align.mjs"))) throw new IOException("Choose tools/read-along, containing align.mjs, after running npm ci in that folder.");
            EngineFolder = folder.Path; StatusText.Text = "Local engine configured. Attach an EPUB and generate alignment.";
        }
        catch (Exception ex) { StatusText.Text = ex.Message; }
    }
    private async void Generate_Click(object sender, RoutedEventArgs e)
    {
        if (_busy || Book is not { } book || _association is not { } association) { StatusText.Text = "Attach an EPUB first."; return; }
        _operation?.Cancel(); var operation = _operation = new CancellationTokenSource(); var token = operation.Token;
        SetBusy(true); var sources = SourcePaths(book); var language = AudioLanguage.Text.Trim();
        _map = null; Passages.ItemsSource = null;
        try
        {
            StatusText.Text = "Starting local CPU alignment. First use downloads the speech model…";
            var progress = new Progress<string>(text => { if (_operation == operation) StatusText.Text = text; });
            var path = await new StorytellerProcessor().GenerateAsync(EngineFolder, Path.Combine(_store.BookFolder(book.Id), "cache"),
                book.Id, association.EpubPath, sources, language, progress, token);
            var updated = new BookAssociation { EpubPath = association.EpubPath, MapPath = path, Status = "ready" };
            // Validate result before replacing the last good association.
            var map = await Task.Run(() => _store.LoadMapAsync(book.Id, updated, sources, token), token);
            token.ThrowIfCancellationRequested();
            await _store.SaveAsync(book.Id, updated, token);
            if (_operation != operation || Book?.Id != book.Id) return;
            _association = updated; _map = map; Passages.ItemsSource = map?.Passages;
            _follow = true; _lastPassage = null; _lastCue = null;
            StatusText.Text = "Ready · reliable word timing · gaps remain unhighlighted";
        }
        catch (OperationCanceledException) { if (_operation == operation) StatusText.Text = "Cancelled. Completed audio analysis can be reused on retry."; }
        catch (Exception ex)
        {
            if (_operation == operation)
            {
                StatusText.Text = ex.Message;
                association.Status = "failed"; association.Error = ex.Message;
                try { await _store.SaveAsync(book.Id, association); } catch { }
            }
        }
        finally { if (_operation == operation) SetBusy(false); }
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) => _operation?.Cancel();
    private void Follow_Click(object sender, RoutedEventArgs e)
    {
        _follow = true; _lastPassage = null;
        ModeLabel.Text = "Following narration"; UpdatePosition();
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
            ModeLabel.Text = active.Cue.Granularity == "word" ? "Following · word timing" : "Following · sentence timing";
        }
        if (_lastCue == cue) return;
        _lastCue = cue;
        foreach (var row in VisibleRows()) row.Highlight(cue);
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
            FindChild<ReadAlongPassage>(a.ItemContainer)?.Highlight(_lastCue));
    }
    private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent is T value) return value;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            if (FindChild<T>(VisualTreeHelper.GetChild(parent, i)) is { } child) return child;
        return null;
    }
}
