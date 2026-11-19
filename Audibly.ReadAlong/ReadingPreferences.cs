namespace Audibly.ReadAlong;

public sealed record ReadingPreferences
{
    public double FontSize { get; init; } = 22;
    public double MiniFontSize { get; init; } = 24;
    public string? TextColor { get; init; }
    public string HighlightColor { get; init; } = "#FFD700";
    public string HighlightTextColor { get; init; } = "#000000";
    public double CaptionOpacity { get; init; } = 0.87;

    public ReadingPreferences Normalize() => this with
    {
        FontSize = double.IsFinite(FontSize) ? Math.Clamp(FontSize, 16, 36) : 22,
        MiniFontSize = double.IsFinite(MiniFontSize) ? Math.Clamp(MiniFontSize, 16, 36) : 24,
        CaptionOpacity = double.IsFinite(CaptionOpacity) ? Math.Clamp(CaptionOpacity, 0.4, 1) : 0.87,
        TextColor = ValidColor(TextColor) ? TextColor : null,
        HighlightColor = ValidColor(HighlightColor) ? HighlightColor : "#FFD700",
        HighlightTextColor = ValidColor(HighlightTextColor) ? HighlightTextColor : "#000000"
    };
    private static bool ValidColor(string? value) => value is { Length: 7 } && value[0] == '#' &&
        uint.TryParse(value.AsSpan(1), System.Globalization.NumberStyles.HexNumber, null, out _);
}
