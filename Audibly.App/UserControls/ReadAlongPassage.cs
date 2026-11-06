using System;
using System.Linq;
using Audibly.ReadAlong;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace Audibly.App.UserControls;

// Only realized ListView rows create inlines; the book is never a control per word.
public sealed class ReadAlongPassage : UserControl
{
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(nameof(Data), typeof(Passage),
        typeof(ReadAlongPassage), new PropertyMetadata(null, (d, _) => ((ReadAlongPassage)d).Render()));
    public Passage? Data { get => (Passage?)GetValue(DataProperty); set => SetValue(DataProperty, value); }
    private readonly RichTextBlock _text = new() { FontSize = 22, TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true };
    public ReadAlongPassage() { Content = _text; Margin = new Thickness(12, 6, 12, 10); }
    private void Render()
    {
        _text.Blocks.Clear(); _text.TextHighlighters.Clear();
        if (Data is not { } passage) return;
        var paragraph = new Paragraph(); int offset = 0;
        foreach (var cue in passage.Cues.OrderBy(c => c.Offset).GroupBy(c => c.Offset).Select(g => g.First()))
        {
            if (cue.Offset < offset) continue;
            if (cue.Offset > offset) paragraph.Inlines.Add(new Run { Text = passage.Text[offset..cue.Offset] });
            var link = new Hyperlink { UnderlineStyle = UnderlineStyle.None };
            link.Inlines.Add(new Run { Text = passage.Text.Substring(cue.Offset, cue.Length) });
            link.Click += async (_, _) =>
            {
                try { await App.PlayerViewModel.SeekReadAlongAsync(cue.SourceIndex, cue.StartSeconds); }
                catch (Exception e) { await Services.DialogService.ShowErrorDialogAsync("Unable to seek", e.Message); }
            };
            paragraph.Inlines.Add(link); offset = cue.Offset + cue.Length;
        }
        if (offset < passage.Text.Length) paragraph.Inlines.Add(new Run { Text = passage.Text[offset..] });
        _text.Blocks.Add(paragraph);
    }
    public void Highlight(Cue? cue)
    {
        _text.TextHighlighters.Clear();
        if (cue is null || Data is null || !Data.Cues.Contains(cue)) return;
        var highlighter = new TextHighlighter
        {
            Background = new SolidColorBrush(Colors.Gold), Foreground = new SolidColorBrush(Colors.Black)
        };
        highlighter.Ranges.Add(new TextRange { StartIndex = cue.Offset, Length = cue.Length });
        _text.TextHighlighters.Add(highlighter);
    }
}
