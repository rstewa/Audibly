using System.Text.Json;

namespace Audibly.ReadAlong;

// Fractions describe only the named stage. Null means work has no measured percentage.
public sealed record AlignmentProgress(string Stage, double? Fraction, string? Detail)
{
    public static bool TryParse(string line, out AlignmentProgress? progress)
    {
        progress = null;
        if (!line.StartsWith("PROGRESS:", StringComparison.Ordinal)) return false;
        try
        {
            var value = JsonSerializer.Deserialize<AlignmentProgress>(line[9..], AlignmentMap.Json);
            if (value is null || string.IsNullOrWhiteSpace(value.Stage) || value.Stage.Length > 120 ||
                value.Fraction is { } f && (!double.IsFinite(f) || f < 0 || f > 1)) return false;
            progress = value;
            return true;
        }
        catch (JsonException) { return false; }
    }
}
