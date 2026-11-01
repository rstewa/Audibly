using System;

namespace Audibly.App.Helpers;

public static class PlaybackSpeedPresets
{
    private static readonly double[] Speeds = [1, 1.1, 1.25, 1.5, 1.75, 2, 2.5, 3, 3.5, 4];

    // Use equally spaced slider positions for the unevenly spaced speed values.
    public static double ToIndex(double speed)
    {
        var nearest = 0;
        for (var i = 1; i < Speeds.Length; i++)
            if (Math.Abs(Speeds[i] - speed) < Math.Abs(Speeds[nearest] - speed)) nearest = i;
        return nearest;
    }

    public static double FromIndex(double index) =>
        Speeds[(int)Math.Clamp(double.IsFinite(index) ? Math.Round(index) : 0, 0, Speeds.Length - 1)];

    public static double Snap(double speed) => FromIndex(ToIndex(speed));
}
