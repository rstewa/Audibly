using System;
using Audibly.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Audibly.App.UserControls;

/// <summary>
///     The read-along pane: virtualized sentence list with active-sentence highlight,
///     word-level karaoke via a reused TextHighlighter, auto-scroll that yields to user
///     scrolling, and tap-to-seek.
/// </summary>
public sealed partial class TranscriptPane : UserControl
{
    private readonly TextHighlighter _wordHighlighter = new();
    private TextBlock? _highlightedTextBlock;
    private bool _programmaticScroll;

    public TranscriptPane()
    {
        InitializeComponent();

        _wordHighlighter.Background = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
        _wordHighlighter.Foreground = (Brush)Application.Current.Resources["TextOnAccentFillColorPrimaryBrush"];

        // Subscribe only while in the visual tree: the view model outlives every pane
        // instance (one is created per PlayerPage navigation), and a stale subscription
        // would drive ItemsRepeater/BringIntoView on an unloaded tree — exceptions there
        // happen inside dispatcher callbacks and fail-fast the app (0xc000027b).
        Loaded += (_, _) =>
        {
            Vm.ActiveSentenceChanged += OnActiveSentenceChanged;
            Vm.ActiveWordChanged += OnActiveWordChanged;
            Vm.ScrollToRequested += OnScrollToRequested;
        };
        Unloaded += (_, _) =>
        {
            Vm.ActiveSentenceChanged -= OnActiveSentenceChanged;
            Vm.ActiveWordChanged -= OnActiveWordChanged;
            Vm.ScrollToRequested -= OnScrollToRequested;
            ClearWordHighlight();
        };
    }

    /// <summary>
    ///     Gets the app-wide transcript view model instance.
    /// </summary>
    public TranscriptViewModel Vm => App.TranscriptVm;

    private async void Sentence_Tapped(object sender, TappedRoutedEventArgs e)
    {
        // ItemsRepeater does not set DataContext for x:Bind templates — the item travels via Tag
        if ((sender as FrameworkElement)?.Tag is TranscriptSegmentViewModel sentence)
            await Vm.SeekToAsync(sentence);
    }

    private void ReturnToPosition_Click(object sender, RoutedEventArgs e)
    {
        Vm.ResumeFollowing();
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            Vm.SearchQuery = sender.Text;
    }

    private async void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        await Vm.GoToNextHitAsync();
    }

    private void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != Windows.System.VirtualKey.Escape) return;

        SearchBox.Text = "";
        Vm.ClearSearch();
        e.Handled = true;
    }

    private async void NextHit_Click(object sender, RoutedEventArgs e)
    {
        await Vm.GoToNextHitAsync();
    }

    private async void PreviousHit_Click(object sender, RoutedEventArgs e)
    {
        await Vm.GoToPreviousHitAsync();
    }

    private void OnScrollToRequested(int index)
    {
        if (index < 0 || index >= Vm.Sentences.Count) return;

        BringSentenceIntoView(index, 0.4);
    }

    /// <summary>
    ///     Scrolls a row into view; runs inside dispatcher callbacks, so any layout-timing
    ///     exception is logged instead of fail-fasting the process.
    /// </summary>
    private void BringSentenceIntoView(int index, double verticalAlignmentRatio)
    {
        if (!IsLoaded) return;

        try
        {
            var element = Repeater.TryGetElement(index) ?? Repeater.GetOrCreateElement(index);
            if (element == null) return;

            _programmaticScroll = true;
            element.StartBringIntoView(new BringIntoViewOptions
            {
                VerticalAlignmentRatio = verticalAlignmentRatio,
                AnimationDesired = true
            });
        }
        catch (Exception e)
        {
            App.ViewModel.LoggingService.LogError(e, false);
        }
    }

    private void OnActiveSentenceChanged(int index)
    {
        ClearWordHighlight();

        if (index < 0 || index >= Vm.Sentences.Count || Vm.IsAutoScrollSuspended || !Vm.FollowPlayback) return;

        BringSentenceIntoView(index, 0.33);
    }

    private void OnActiveWordChanged(int sentenceIndex, int charOffset, int charLength)
    {
        var textBlock = (Repeater.TryGetElement(sentenceIndex) as Border)?.Child as TextBlock;
        if (textBlock == null)
        {
            ClearWordHighlight();
            return;
        }

        // TextBlock does not repaint when an attached highlighter's Ranges mutate —
        // the highlighter must be detached and re-attached for every word change.
        _highlightedTextBlock?.TextHighlighters.Remove(_wordHighlighter);
        _wordHighlighter.Ranges.Clear();
        if (charOffset + charLength <= textBlock.Text.Length)
            _wordHighlighter.Ranges.Add(new TextRange(charOffset, charLength));
        textBlock.TextHighlighters.Add(_wordHighlighter);
        _highlightedTextBlock = textBlock;
    }

    private void ClearWordHighlight()
    {
        _wordHighlighter.Ranges.Clear();
        _highlightedTextBlock?.TextHighlighters.Remove(_wordHighlighter);
        _highlightedTextBlock = null;
    }

    private void Scroll_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_programmaticScroll)
        {
            if (!e.IsIntermediate) _programmaticScroll = false;
            return;
        }

        if (!e.IsIntermediate && Vm.FollowPlayback) Vm.IsAutoScrollSuspended = true;
    }
}
