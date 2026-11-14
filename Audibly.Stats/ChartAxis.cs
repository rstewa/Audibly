namespace Audibly.Stats;

public static class ChartAxis
{
    private const int Intervals = 4;
    private const double Minute = 60, Hour = 3600;

    private static readonly double[] Steps =
    [
        5 * Minute, 10 * Minute, 15 * Minute, 30 * Minute, Hour, 2 * Hour, 3 * Hour, 6 * Hour, 12 * Hour, 24 * Hour,
        50 * Hour, 100 * Hour, 250 * Hour, 500 * Hour, 1000 * Hour
    ];

    /// <summary>A value axis from zero with four even steps that fits the largest bar.</summary>
    public static (double MaxSeconds, IReadOnlyList<double> TickSeconds) For(double largestValueSeconds)
    {
        double step;
        if (!double.IsFinite(largestValueSeconds) || largestValueSeconds <= 0) step = 15 * Minute;
        else if (largestValueSeconds > Steps[^1] * Intervals)
            step = Math.Ceiling(largestValueSeconds / Intervals / Steps[^1]) * Steps[^1];
        else step = Steps.First(candidate => candidate * Intervals >= largestValueSeconds);

        return (step * Intervals, Enumerable.Range(0, Intervals + 1).Select(i => i * step).ToList());
    }
}
