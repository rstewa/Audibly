using System;
using System.Linq;
using System.Collections.Generic;
using Audibly.ReadAlong;
using Audibly.App.Helpers;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Data;

namespace Audibly.App.UserControls;

// Only realized ListView rows create inlines; the book is never a control per word.
public sealed class ReadAlongPassage : UserControl
{
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(nameof(Data), typeof(Passage),
        typeof(ReadAlongPassage), new PropertyMetadata(null, (d, _) => ((ReadAlongPassage)d).Render()));
    public Passage? Data { get => (Passage?)GetValue(DataProperty); set => SetValue(DataProperty, value); }
    private readonly RichTextBlock _text = new() { FontSize = 22, TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true };
    private Cue? _active;
    private readonly Dictionary<string, Hyperlink> _links = new();
    public bool MiniMode { get; set; }
    public ReadAlongPassage()
    {
        Content = _text; Margin = new Thickness(8, 4, 8, 12);
        Loaded += (_, _) => { ReadAlongAppearanceSettings.Changed += AppearanceChanged; ApplyAppearance(); };
        Unloaded += (_, _) => ReadAlongAppearanceSettings.Changed -= AppearanceChanged;
        ActualThemeChanged += (_, _) => ApplyAppearance();
    }
    public double ReadingFontSize { get => _text.FontSize; set { _text.FontSize = Math.Clamp(value, 16, 36); _text.LineHeight = _text.FontSize * 1.5; } }
    private void AppearanceChanged(object? sender, EventArgs e) => ApplyAppearance();
    private void ApplyAppearance()
    {
        var prefs = ReadAlongAppearanceSettings.Current;
        ReadingFontSize = MiniMode ? prefs.MiniFontSize : prefs.FontSize;
        _text.Foreground = new SolidColorBrush(prefs.TextColor is { } color ? ReadAlongAppearanceSettings.Color(color)
            : ActualTheme == ElementTheme.Dark ? Colors.White : Colors.Black);
        Highlight(_active);
    }
    private void Render()
    {
        _text.Blocks.Clear(); _text.TextHighlighters.Clear(); _links.Clear(); _active = null;
        if (Data is not { } passage) return;
        var paragraph = new Paragraph(); int offset = 0;
        foreach (var group in passage.Cues.OrderBy(c => c.Offset).GroupBy(c => c.Offset))
        {
            var cue = group.First();
            if (cue.Offset < offset) continue;
            if (cue.Offset > offset) paragraph.Inlines.Add(new Run { Text = passage.Text[offset..cue.Offset] });
            var link = new Hyperlink { UnderlineStyle = UnderlineStyle.None };
            BindingOperations.SetBinding(link, Hyperlink.ForegroundProperty, new Binding { Source = _text, Path = new PropertyPath("Foreground") });
            link.Inlines.Add(new Run { Text = passage.Text.Substring(cue.Offset, cue.Length) });
            link.Click += async (_, _) =>
            {
                var target = group.FirstOrDefault(c => c.SourceIndex == App.PlayerViewModel.NowPlaying?.CurrentSourceFileIndex) ?? cue;
                try { await App.PlayerViewModel.SeekReadAlongAsync(target.SourceIndex, target.StartSeconds); }
                catch (Exception e) { await Services.DialogService.ShowErrorDialogAsync("Unable to seek", e.Message); }
            };
            foreach (var entry in group) _links[entry.Id] = link;
            paragraph.Inlines.Add(link); offset = cue.Offset + cue.Length;
        }
        if (offset < passage.Text.Length) paragraph.Inlines.Add(new Run { Text = passage.Text[offset..] });
        _text.Blocks.Add(paragraph);
    }
    public void BringWordIntoView(Cue? cue)
    {
        if (!_text.IsLoaded || cue is null || !_links.TryGetValue(cue.Id, out var link)) return;
        var rect = link.ContentStart.GetCharacterRect(LogicalDirection.Forward);
        if (rect.Height <= 0) return;
        rect.Width = Math.Max(1, rect.Width);
        _text.StartBringIntoView(new BringIntoViewOptions { TargetRect = rect, AnimationDesired = false });
    }
    public void Highlight(Cue? cue)
    {
        _active = cue;
        _text.TextHighlighters.Clear();
        if (cue is null || Data is null || !Data.Cues.Contains(cue)) return;
        var highlighter = new TextHighlighter
        {
            Background = new SolidColorBrush(ReadAlongAppearanceSettings.Color(ReadAlongAppearanceSettings.Current.HighlightColor)),
            Foreground = new SolidColorBrush(ReadAlongAppearanceSettings.Color(ReadAlongAppearanceSettings.Current.HighlightTextColor))
        };
        highlighter.Ranges.Add(new TextRange { StartIndex = cue.Offset, Length = cue.Length });
        _text.TextHighlighters.Add(highlighter);
    }
}
