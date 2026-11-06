using System;
using Audibly.App.Helpers;
using Audibly.ReadAlong;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Audibly.App.UserControls;

public sealed partial class ReadAlongAppearance : UserControl
{
    public bool MiniMode { get; set; }
    private bool _updating = true;
    public ReadAlongAppearance() { InitializeComponent(); }
    private void Appearance_Opened(object sender, object e) => Refresh();
    private void Refresh()
    {
        _updating = true;
        var prefs = ReadAlongAppearanceSettings.Current;
        TextSize.Value = MiniMode ? prefs.MiniFontSize : prefs.FontSize;
        CaptionOpacity.Value = prefs.CaptionOpacity * 100;
        var color = ColorTarget.SelectedIndex switch { 1 => prefs.HighlightColor, 2 => prefs.HighlightTextColor,
            _ => prefs.TextColor ?? (MiniMode || ActualTheme == ElementTheme.Dark ? "#FFFFFF" : "#000000") };
        ReadingColor.Color = ReadAlongAppearanceSettings.Color(color);
        _updating = false;
    }
    private void ColorTarget_Changed(object sender, SelectionChangedEventArgs e) { if (!_updating) Refresh(); }
    private void ReadingColor_Changed(ColorPicker sender, ColorChangedEventArgs args)
    {
        if (_updating) return;
        var c = args.NewColor; var hex = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        var prefs = ReadAlongAppearanceSettings.Current;
        ReadAlongAppearanceSettings.Update(ColorTarget.SelectedIndex switch
        { 1 => prefs with { HighlightColor = hex }, 2 => prefs with { HighlightTextColor = hex }, _ => prefs with { TextColor = hex } });
    }
    private void TextSize_Changed(object sender, RangeBaseValueChangedEventArgs args)
    {
        if (_updating) return;
        var prefs = ReadAlongAppearanceSettings.Current;
        ReadAlongAppearanceSettings.Update(MiniMode ? prefs with { MiniFontSize = args.NewValue } : prefs with { FontSize = args.NewValue });
    }
    private void CaptionOpacity_Changed(object sender, RangeBaseValueChangedEventArgs args)
    { if (!_updating) ReadAlongAppearanceSettings.Update(ReadAlongAppearanceSettings.Current with { CaptionOpacity = args.NewValue / 100 }); }
    private void Reset_Click(object sender, RoutedEventArgs args)
    { ReadAlongAppearanceSettings.Update(new ReadingPreferences()); Refresh(); }
}
