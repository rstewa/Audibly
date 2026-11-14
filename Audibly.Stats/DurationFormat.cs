using System.Globalization;

namespace Audibly.Stats;

public static class DurationFormat
{
    /// <summary>"5h 4m", or "43m" under an hour. Rounded down to whole minutes.</summary>
    public static string Short(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 60) return "0m";
        var minutes = (long)Math.Floor(seconds / 60);
        return minutes < 60
            ? string.Create(CultureInfo.InvariantCulture, $"{minutes}m")
            : string.Create(CultureInfo.InvariantCulture, $"{minutes / 60}h {minutes % 60}m");
    }
}
