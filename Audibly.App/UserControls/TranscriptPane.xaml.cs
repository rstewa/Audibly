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

        Vm.ActiveSentenceChanged += OnActiveSentenceChanged;
        Vm.ActiveWordChanged += OnActiveWordChanged;
    }

    /// <summary>
    ///     Gets the app-wide transcript view model instance.
    /// </summary>
    public TranscriptViewModel Vm => App.TranscriptVm;

    private async void Sentence_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is TranscriptSegmentViewModel sentence)
            await Vm.SeekToAsync(sentence);
    }

    private void ReturnToPosition_Click(object sender, RoutedEventArgs e)
    {
        Vm.ResumeFollowing();
    }

    private void OnActiveSentenceChanged(int index)
    {
        ClearWordHighlight();

        if (index < 0 || Vm.IsAutoScrollSuspended || !Vm.FollowPlayback) return;

        var element = Repeater.TryGetElement(index) ?? Repeater.GetOrCreateElement(index);
        if (element == null) return;

        _programmaticScroll = true;
        element.StartBringIntoView(new BringIntoViewOptions
        {
            VerticalAlignmentRatio = 0.33,
            AnimationDesired = true
        });
    }

    private void OnActiveWordChanged(int sentenceIndex, int charOffset, int charLength)
    {
        var textBlock = (Repeater.TryGetElement(sentenceIndex) as Border)?.Child as TextBlock;
        if (textBlock == null)
        {
            ClearWordHighlight();
            return;
        }

        if (!ReferenceEquals(_highlightedTextBlock, textBlock))
        {
            ClearWordHighlight();
            textBlock.TextHighlighters.Add(_wordHighlighter);
            _highlightedTextBlock = textBlock;
        }

        _wordHighlighter.Ranges.Clear();
        if (charOffset + charLength <= textBlock.Text.Length)
            _wordHighlighter.Ranges.Add(new TextRange(charOffset, charLength));
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
