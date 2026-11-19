// Author: rstewa · https://github.com/rstewa
// Updated: 07/30/2025

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.Storage;
using Windows.System;
using Audibly.App.Extensions;
using Audibly.App.Helpers;
using Audibly.App.UserControls;
using Audibly.App.ViewModels;
using Audibly.ReadAlong;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;
using Constants = Audibly.App.Helpers.Constants;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace Audibly.App.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class NewMiniPlayerPage : Page
{
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private CancellationTokenSource? _playbackSpeedFlyoutCts;
    private CancellationTokenSource? _readingLoad;
    private readonly DispatcherTimer _readingTimer = new() { Interval = TimeSpan.FromMilliseconds(50) };
    private readonly ReadAlongStore _readingStore = new(ApplicationData.Current.LocalFolder.Path);
    private AlignmentMap? _readingMap;
    private Window? _miniWindow;
    private string? _lastCaptionCue;
    private Passage? _lastMiniPassage;
    private bool _followingNarration = true;
    private ScrollViewer? _transcriptScroll;
    private double _captionFontSize = 24;
    private bool _dragging;
    private PointInt32 _dragWindowStart;
    private PointInt32 _dragPointerStart;

    public NewMiniPlayerPage()
    {
        InitializeComponent();
        MiniReadAlongToggle.IsChecked = ApplicationData.Current.LocalSettings.Values["MiniReadAlongEnabled"] is not false;
        _captionFontSize = ReadAlongAppearanceSettings.Current.MiniFontSize;
        MiniTranscript.AddHandler(PointerWheelChangedEvent, new PointerEventHandler(MiniTranscript_Wheel), true);
        MiniTranscript.AddHandler(PointerPressedEvent, new PointerEventHandler(MiniTranscript_Pressed), true);
        MiniTranscript.KeyDown += MiniTranscript_KeyDown;
        _readingTimer.Tick += (_, _) => UpdateCaption();
        Loaded += ReadingLoaded;
        Unloaded += ReadingUnloaded;
        KeyDown += NewMiniPlayerPage_KeyDown;

        // need this to show the slider when the user increases or decreases the speed with the keyboard shortcuts
        PlaybackSpeedSliderFlyout.Opened += (s, e) => { PlaybackSpeedSlider.Focus(FocusState.Programmatic); };

        ProgressSlider.AddHandler(PointerPressedEvent,
            new PointerEventHandler(Slider_OnPointerPressed), true);
        ProgressSlider.AddHandler(PointerReleasedEvent,
            new PointerEventHandler(Slider_OnPointerReleased), true);
        ProgressSlider.AddHandler(PointerCanceledEvent,
            new PointerEventHandler(Slider_OnPointerReleased), true);
    }

    /// <summary>
    ///     Gets the app-wide ViewModel instance.
    /// </summary>
    public MainViewModel ViewModel => App.ViewModel;

    /// <summary>
    ///     Gets the app-wide PlayerViewModel instance.
    /// </summary>
    public PlayerViewModel PlayerViewModel => App.PlayerViewModel;

    public void ConfigureWindow(Window window)
    {
        _miniWindow = window;
        ApplyReadingVisibility();
    }
    private async void ReadingLoaded(object sender, RoutedEventArgs e)
    {
        PlayerViewModel.PropertyChanged += ReadingPlayerChanged;
        ReadAlongAppearanceSettings.Changed += ReadingAppearanceChanged;
        ApplyReadingAppearance();
        _readingTimer.Start();
        await LoadReadingAsync();
    }
    private void ReadingUnloaded(object sender, RoutedEventArgs e)
    {
        PlayerViewModel.PropertyChanged -= ReadingPlayerChanged;
        ReadAlongAppearanceSettings.Changed -= ReadingAppearanceChanged;
        _readingTimer.Stop(); _readingLoad?.Cancel();
    }
    private async void ReadingPlayerChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlayerViewModel.NowPlaying)) await LoadReadingAsync();
        else if (e.PropertyName == nameof(PlayerViewModel.ReadAlongDocument))
        {
            _readingMap = PlayerViewModel.ReadAlongDocument;
            ClearCaption(); UpdateCaption();
        }
    }
    private void ClearCaption()
    {
        _lastCaptionCue = null; _lastMiniPassage = null;
        MiniTranscript.ItemsSource = null;
        _followingNarration = true;
        MiniFollowButton.Visibility = Visibility.Collapsed;
    }
    private void ShowReadingHint(string text, bool setup)
    {
        MiniHintText.Text = text;
        MiniReadingHint.Visibility = Visibility.Visible;
        MiniSetupButton.Visibility = setup ? Visibility.Visible : Visibility.Collapsed;
    }
    private async Task LoadReadingAsync()
    {
        _readingLoad?.Cancel(); _readingLoad?.Dispose();
        var operation = _readingLoad = new CancellationTokenSource(); var token = operation.Token;
        _readingMap = null; ClearCaption();
        if (MiniReadAlongToggle.IsChecked != true) return;
        if (PlayerViewModel.NowPlaying is not { } book)
        {
            MiniReadAlongStatus.Text = "No audiobook open";
            ShowReadingHint("Open an audiobook in the main player.", true); return;
        }
        try
        {
            MiniReadAlongStatus.Text = "";
            ShowReadingHint("Opening your ebook…", false);
            if (PlayerViewModel.ReadAlongDocument is { } shared && shared.AudiobookId == book.Id)
                _readingMap = shared;
            else
            {
                var association = await _readingStore.LoadAsync(book.Id, token);
                if (association is null || association.MapPath is null)
                {
                    token.ThrowIfCancellationRequested();
                    MiniReadAlongStatus.Text = "";
                    ShowReadingHint("Add an ebook and set up Read Along to see the words here.", true); return;
                }
                var sources = book.SourcePaths.Select(s => s.FilePath).ToArray();
                var map = await Task.Run(async () =>
                {
                    var saved = await _readingStore.LoadMapAsync(book.Id, association, sources, token);
                    return saved is null ? null : await ReadingLayout.CreateAsync(saved, token);
                }, token);
                token.ThrowIfCancellationRequested();
                if (PlayerViewModel.NowPlaying?.Id != book.Id || _readingLoad != operation) return;
                _readingMap = map;
                PlayerViewModel.ReadAlongDocument = map;
            }
            token.ThrowIfCancellationRequested();
            if (_readingMap is null) { ShowReadingHint("Set up Read Along to see the words here.", true); return; }
            UpdateCaption();
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            if (_readingLoad != operation) return;
            MiniReadAlongStatus.Text = "Read Along unavailable";
            ShowReadingHint("Open Read Along to check this book's ebook settings.", true);
        }
    }
    private void UpdateCaption()
    {
        if (_miniWindow?.AppWindow.IsVisible == false) return;
        if (MiniReadAlongToggle.IsChecked != true || _readingMap is null || PlayerViewModel.NowPlaying is not { } book || book.Id != _readingMap.AudiobookId) return;
        if ((_miniWindow?.AppWindow.Presenter as OverlappedPresenter)?.State == OverlappedPresenterState.Minimized) return;
        var source = book.CurrentSourceFileIndex;
        var position = PlayerViewModel.CurrentPosition.TotalSeconds;
        var active = _readingMap.Find(source, position);
        if (!ReferenceEquals(MiniTranscript.ItemsSource, _readingMap.Passages)) MiniTranscript.ItemsSource = _readingMap.Passages;
        MiniReadingHint.Visibility = Visibility.Collapsed;
        var context = active ?? _readingMap.FindContext(source, position);
        var status = !_followingNarration ? "Reading earlier text" : context is null ? "No word timing here" : "Scroll to read back";
        if (MiniReadAlongStatus.Text != status) MiniReadAlongStatus.Text = status;
        if (_followingNarration && context is { } current && current.Passage != _lastMiniPassage)
        {
            MiniTranscript.ScrollIntoView(current.Passage, ScrollIntoViewAlignment.Leading);
            _lastMiniPassage = current.Passage;
        }
        var cue = active?.Cue;
        if (_lastCaptionCue == cue?.Id) return;
        _lastCaptionCue = cue?.Id;
        foreach (var row in TranscriptRows())
        {
            row.Highlight(cue);
            if (_followingNarration) row.BringWordIntoView(cue);
        }
    }
    private void SuspendMiniFollowing()
    {
        if (_readingMap is null || !_followingNarration) return;
        _followingNarration = false;
        MiniFollowButton.Visibility = Visibility.Visible;
        MiniReadAlongStatus.Text = "Reading earlier text";
        // Browsing is read-only: selecting old text must not accidentally seek.
        foreach (var row in TranscriptRows()) row.IsSeekEnabled = false;
        _lastCaptionCue = null;
    }
    private void MiniFollow_Click(object sender, RoutedEventArgs e)
    {
        _followingNarration = true; _lastMiniPassage = null; _lastCaptionCue = null;
        MiniFollowButton.Visibility = Visibility.Collapsed;
        foreach (var row in TranscriptRows()) row.IsSeekEnabled = true;
        UpdateCaption();
    }
    private void MiniTranscript_Wheel(object sender, PointerRoutedEventArgs e)
    {
        if (e.GetCurrentPoint(MiniTranscript).Properties.MouseWheelDelta != 0) SuspendMiniFollowing();
        // Let the native ListView handle wheel/trackpad scrolling normally.
    }
    private void MiniTranscript_Pressed(object sender, PointerRoutedEventArgs e)
    {
        for (var node = e.OriginalSource as DependencyObject; node is not null && node != MiniTranscript;)
        {
            if (node is ScrollBar) { SuspendMiniFollowing(); return; }
            if (node is not UIElement) break;
            node = VisualTreeHelper.GetParent(node);
        }
    }
    private void MiniTranscript_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key is VirtualKey.Up or VirtualKey.Down or VirtualKey.PageUp or VirtualKey.PageDown or VirtualKey.Home or VirtualKey.End)
            SuspendMiniFollowing();
    }
    private void MiniTranscript_Loaded(object sender, RoutedEventArgs e)
    {
        _transcriptScroll = FindTranscriptChild<ScrollViewer>(MiniTranscript);
        if (_transcriptScroll is not null) _transcriptScroll.DirectManipulationStarted += MiniTranscript_ManipulationStarted;
    }
    private void MiniTranscript_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_transcriptScroll is not null) _transcriptScroll.DirectManipulationStarted -= MiniTranscript_ManipulationStarted;
        _transcriptScroll = null;
    }
    private void MiniTranscript_ManipulationStarted(object? sender, object e) => SuspendMiniFollowing();
    private System.Collections.Generic.IEnumerable<ReadAlongPassage> TranscriptRows()
    {
        if (MiniTranscript.ItemsPanelRoot is not { } panel) yield break;
        foreach (var child in panel.Children)
            if (FindTranscriptChild<ReadAlongPassage>(child) is { } row) yield return row;
    }
    private void MiniTranscript_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.InRecycleQueue) return;
        args.RegisterUpdateCallback((_, a) =>
        {
            if (FindTranscriptChild<ReadAlongPassage>(a.ItemContainer) is not { } row) return;
            row.IsSeekEnabled = _followingNarration;
            var cue = _readingMap?.Find(PlayerViewModel.NowPlaying?.CurrentSourceFileIndex ?? -1, PlayerViewModel.CurrentPosition.TotalSeconds)?.Cue;
            row.Highlight(cue);
            if (_followingNarration) row.BringWordIntoView(cue);
        });
    }
    private static T? FindTranscriptChild<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent is T value) return value;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            if (FindTranscriptChild<T>(VisualTreeHelper.GetChild(parent, i)) is { } child) return child;
        return null;
    }
    private void ReadingAppearanceChanged(object? sender, EventArgs e) => ApplyReadingAppearance();
    private void ApplyReadingAppearance()
    {
        var prefs = ReadAlongAppearanceSettings.Current;
        MiniReadingPane.Background = new SolidColorBrush(Windows.UI.Color.FromArgb((byte)Math.Round(prefs.CaptionOpacity * 255), 24, 24, 24));
        if (_captionFontSize != prefs.MiniFontSize)
        {
            _captionFontSize = prefs.MiniFontSize;
            _lastCaptionCue = null; _lastMiniPassage = null;
            ApplyReadingVisibility();
            UpdateCaption();
        }
    }
    private void ApplyReadingVisibility()
    {
        var enabled = MiniReadAlongToggle.IsChecked == true;
        MiniReadingPane.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        MiniCaptionTools.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        if (_miniWindow is not { } window) return;
        var scale = Win32.GetDpiForWindow(WindowNative.GetWindowHandle(window)) / 96.0;
        // Resize without moving the window, including when pinned on top.
        var height = enabled ? Math.Max(320, 180 + _captionFontSize * 4.5) : 104;
        window.AppWindow.Resize(new SizeInt32((int)Math.Ceiling(688 * scale), (int)Math.Ceiling(height * scale)));
    }
    private async void MiniReadAlong_Click(object sender, RoutedEventArgs e)
    {
        ApplicationData.Current.LocalSettings.Values["MiniReadAlongEnabled"] = MiniReadAlongToggle.IsChecked == true;
        ApplyReadingVisibility();
        if (MiniReadAlongToggle.IsChecked == true && _readingMap is null) await LoadReadingAsync();
        else if (MiniReadAlongToggle.IsChecked != true) _readingLoad?.Cancel();
        else UpdateCaption();
    }
    private void ChangeCaptionSize(double delta)
    {
        ReadAlongAppearanceSettings.Update(ReadAlongAppearanceSettings.Current with { MiniFontSize = _captionFontSize + delta });
    }
    private void SmallerText_Click(object sender, RoutedEventArgs e) => ChangeCaptionSize(-2);
    private void LargerText_Click(object sender, RoutedEventArgs e) => ChangeCaptionSize(2);
    private void OpenReadAlong_Click(object sender, RoutedEventArgs e)
    {
        PlayerViewModel.OpenReadAlong();
        WindowHelper.RestoreMainWindow(); WindowHelper.GetMainWindow()?.Activate(); WindowHelper.HideMiniPlayer();
    }
    private void DragHandle_Pressed(object sender, PointerRoutedEventArgs e)
    {
        if (_miniWindow is null || !e.GetCurrentPoint(MiniDragHandle).Properties.IsLeftButtonPressed) return;
        PInvoke.User32.GetCursorPos(out var point);
        _dragPointerStart = new PointInt32(point.x, point.y); _dragWindowStart = _miniWindow.AppWindow.Position;
        _dragging = MiniDragHandle.CapturePointer(e.Pointer); e.Handled = true;
    }
    private void DragHandle_Moved(object sender, PointerRoutedEventArgs e)
    {
        if (!_dragging || _miniWindow is null) return;
        PInvoke.User32.GetCursorPos(out var point);
        _miniWindow.AppWindow.Move(new PointInt32(_dragWindowStart.X + point.x - _dragPointerStart.X, _dragWindowStart.Y + point.y - _dragPointerStart.Y));
        e.Handled = true;
    }
    private void DragHandle_Released(object sender, PointerRoutedEventArgs e)
    {
        _dragging = false; MiniDragHandle.ReleasePointerCaptures(); e.Handled = true;
    }

    private void NewMiniPlayerPage_KeyDown(object sender, KeyRoutedEventArgs args)
    {
        var key = args.Key;
        if (key == (VirtualKey)219) // Open bracket '['
        {
            HandleSpeedDecrease();
            ClosePlaybackSpeedFlyout();
            args.Handled = true;
        }
        else if (key == (VirtualKey)221) // Close bracket ']'
        {
            HandleSpeedIncrease();
            ClosePlaybackSpeedFlyout();
            args.Handled = true;
        }
        else if (key == (VirtualKey)0xDC) // Backslash '\'
        {
            ResetPlaybackSpeed();
            ClosePlaybackSpeedFlyout();
            args.Handled = true;
        }
    }

    private void ClosePlaybackSpeedFlyout()
    {
        // Cancel any previous close operation
        _playbackSpeedFlyoutCts?.Cancel();
        _playbackSpeedFlyoutCts = new CancellationTokenSource();
        var token = _playbackSpeedFlyoutCts.Token;

        _dispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                await Task.Delay(2000, token);
                if (PlaybackSpeedSliderFlyout.IsOpen)
                    PlaybackSpeedSliderFlyout.Hide();
            }
            catch (TaskCanceledException)
            {
                // Ignore cancellation
            }
        });
    }

    private void ResetPlaybackSpeed()
    {
        PlaybackSpeedSliderFlyout.ShowAt(PlaybackSpeedButton);
        PlaybackSpeedSlider.Value = Constants.PlaybackSpeedDefault;
    }

    private void HandleSpeedIncrease()
    {
        PlaybackSpeedSliderFlyout.ShowAt(PlaybackSpeedButton);
        var newValue = PlaybackSpeedSlider.Value + Constants.PlaybackSpeedIncrement;
        if (newValue >= Constants.PlaybackSpeedMaximum) newValue = Constants.PlaybackSpeedMaximum;
        PlaybackSpeedSlider.Value = newValue;
    }

    private void HandleSpeedDecrease()
    {
        PlaybackSpeedSliderFlyout.ShowAt(PlaybackSpeedButton);
        var newValue = PlaybackSpeedSlider.Value - Constants.PlaybackSpeedIncrement;
        if (newValue <= Constants.PlaybackSpeedMinimum) newValue = Constants.PlaybackSpeedMinimum;
        PlaybackSpeedSlider.Value = newValue;
    }

    private bool _isPointerInteracting;
    private double _pendingSeekValue;

    private void Slider_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isPointerInteracting = true;
        PlayerViewModel.IsUserSeeking = true;
    }

    private async void Slider_OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_isPointerInteracting) return;
        _isPointerInteracting = false;

        await PlayerViewModel.SeekToPositionAsync(_pendingSeekValue);
    }

    private async void Slider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        _pendingSeekValue = e.NewValue;

        // If not dragging (e.g. a tap/click to seek), commit immediately
        if (!_isPointerInteracting && PlayerViewModel.IsUserSeeking)
        {
            await PlayerViewModel.SeekToPositionAsync(e.NewValue);
        }
    }

    private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        var slider = sender as Slider;
        if (slider == null || !IsLoaded) return;

        PlayerViewModel.UpdateVolume(slider.Value);
    }

    private void PlaybackSpeedSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        var slider = sender as Slider;
        if (slider == null || !IsLoaded) return;

        PlayerViewModel.UpdatePlaybackSpeed(slider.Value);
    }

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        var window = WindowHelper.GetMiniPlayerWindow();
        if (window == null) return;

        PinButton.Visibility = Visibility.Collapsed;
        UnpinButton.Visibility = Visibility.Visible;

        window.SetWindowAlwaysOnTop(true);
    }

    private void UnpinButton_Click(object sender, RoutedEventArgs e)
    {
        var window = WindowHelper.GetMiniPlayerWindow();
        if (window == null) return;

        UnpinButton.Visibility = Visibility.Collapsed;
        PinButton.Visibility = Visibility.Visible;

        window.SetWindowAlwaysOnTop(false);
    }

    private void BackToLibraryButton_Click(object sender, RoutedEventArgs e)
    {
        WindowHelper.RestoreMainWindow();
        WindowHelper.HideMiniPlayer();
    }

    private void TimerMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && int.TryParse(item.Tag?.ToString(), out var seconds))
            PlayerViewModel.SetTimer(seconds);
    }

    private void CustomTimerMenuItem_Click(object sender, RoutedEventArgs e)
    {
        // Create the popup
        var timerPopup = new CustomTimerPopup();

        // Find the timer button and position the popup above it

        // get timer button's position using its x:name
        var timerButton = TimerButton;
        if (timerButton == null)
        {
            // Fallback if the button is not found
            timerPopup.Show(XamlRoot);
            return;
        }

        // Show the popup above the button
        timerPopup.ShowAbove(timerButton, XamlRoot);
    }

    private void CancelTimerMenuItem_Click(object sender, RoutedEventArgs e)
    {
        PlayerViewModel.SetTimer(0);
    }

    private void EndOfChapterTimerMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (PlayerViewModel.NowPlaying?.CurrentChapter == null) return;

        var endTime = PlayerViewModel.NowPlaying.CurrentChapter.EndTime;
        var currentPosition = PlayerViewModel.CurrentPosition.TotalMilliseconds;
        var timerDuration = endTime - currentPosition;

        // convert to seconds
        timerDuration = timerDuration.ToSeconds().ToInt();

        if (timerDuration > 0) PlayerViewModel.SetTimer(timerDuration);
    }
}
