using System.Globalization;

namespace Audibly.Stats;

public sealed record Totals(double ListenedSeconds, double SavedSeconds);

/// <summary>Listening within one day, month or year; <see cref="Start" /> is its first day.</summary>
public sealed record PeriodTotal(string Label, DateOnly Start, double ListenedSeconds);

public static class StatsSummary
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public static Totals Total(StatsData data) => Sum(data.Slices);

    public static Totals ForBook(StatsData data, Guid audiobookId) =>
        Sum(data.Slices.Where(slice => slice.AudiobookId == audiobookId));

    /// <summary>The most recent days with any listening, oldest first; days without listening are skipped.</summary>
    public static IReadOnlyList<PeriodTotal> LastListenedDays(StatsData data, int count = 5) =>
        data.Slices.GroupBy(slice => slice.Day)
            .Select(day => new PeriodTotal(day.Key.ToString("ddd d", Invariant), day.Key, day.Sum(s => s.ListenedSeconds)))
            .Where(day => day.ListenedSeconds > 0)
            .OrderByDescending(day => day.Start).Take(Math.Max(0, count))
            .OrderBy(day => day.Start).ToList();

    /// <summary>Every month from the first to the last one with data; months in between count as zero.</summary>
    public static IReadOnlyList<PeriodTotal> Months(StatsData data)
    {
        if (data.Slices.Count == 0) return [];
        var sums = data.Slices.GroupBy(slice => new DateOnly(slice.Day.Year, slice.Day.Month, 1))
            .ToDictionary(month => month.Key, month => month.Sum(s => s.ListenedSeconds));
        var first = sums.Keys.Min();
        var last = sums.Keys.Max();
        var format = first.Year == last.Year ? "MMM" : "MMM yy";
        var months = new List<PeriodTotal>();
        for (var month = first; month <= last; month = month.AddMonths(1))
            months.Add(new PeriodTotal(month.ToString(format, Invariant), month, sums.GetValueOrDefault(month)));
        return months;
    }

    /// <summary>Every year from the first to the last one with data.</summary>
    public static IReadOnlyList<PeriodTotal> Years(StatsData data)
    {
        if (data.Slices.Count == 0) return [];
        var sums = data.Slices.GroupBy(slice => slice.Day.Year)
            .ToDictionary(year => year.Key, year => year.Sum(s => s.ListenedSeconds));
        return Enumerable.Range(sums.Keys.Min(), sums.Keys.Max() - sums.Keys.Min() + 1)
            .Select(year => new PeriodTotal(year.ToString(Invariant), new DateOnly(year, 1, 1), sums.GetValueOrDefault(year)))
            .ToList();
    }

    public static double Average(IReadOnlyList<PeriodTotal> periods) =>
        periods.Count == 0 ? 0 : periods.Average(period => period.ListenedSeconds);

    private static Totals Sum(IEnumerable<ListeningSlice> slices)
    {
        double listened = 0, saved = 0;
        foreach (var slice in slices)
        {
            listened += slice.ListenedSeconds;
            saved += slice.SavedSeconds;
        }

        return new Totals(listened, saved);
    }
}
