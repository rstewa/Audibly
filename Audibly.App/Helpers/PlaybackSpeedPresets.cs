using System;

namespace Audibly.App.Helpers;

public static class PlaybackSpeedPresets
{
    public const double Minimum = 0.5;
    public const double Maximum = 4.0;
    public const double Step = 0.05;

    /// <summary>Quick-pick speeds shown in the speed flyout.</summary>
    public static readonly double[] QuickPicks = [1, 1.25, 1.5, 1.75, 2, 2.5, 3];

    public static int StepCount => (int)Math.Round((Maximum - Minimum) / Step);

    // Sliders work on whole-number steps so floating point drift never produces 2.7000000001.
    public static double ToIndex(double speed) => Math.Round((Snap(speed) - Minimum) / Step);

    public static double FromIndex(double index) =>
        Snap(Minimum + Math.Clamp(double.IsFinite(index) ? Math.Round(index) : 0, 0, StepCount) * Step);

    public static double Snap(double speed)
    {
        if (!double.IsFinite(speed)) return 1.0;
        var snapped = Math.Round(Math.Round(speed / Step) * Step, 2);
        return Math.Clamp(snapped, Minimum, Maximum);
    }

    public static string Format(double speed) => $"{Snap(speed):0.0#}×";
}
