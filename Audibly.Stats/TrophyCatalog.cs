using System.Globalization;

namespace Audibly.Stats;

/// <param name="Glyph">A Segoe Fluent Icons character shown on the badge.</param>
/// <param name="Thresholds">The values that unlock bronze, silver and gold.</param>
/// <param name="Goal">Sentence naming the next threshold as {0}.</param>
/// <param name="Progress">Sentence naming the current value as {0}.</param>
public sealed record TrophyDefinition(string Id, string Name, string Glyph, double[] Thresholds, string Goal, string Progress);

public sealed record TrophyInput(StatsData Data, int LibraryCount);

/// <param name="Tier">0 locked, 1 bronze, 2 silver, 3 gold.</param>
public sealed record TrophyState(TrophyDefinition Definition, double Value, int Tier)
{
    public double? NextThreshold => Tier < Definition.Thresholds.Length ? Definition.Thresholds[Tier] : null;

    public string GoalText => NextThreshold is { } next
        ? string.Format(CultureInfo.InvariantCulture, Definition.Goal, Number(next))
        : "All tiers unlocked.";

    public string ProgressText => string.Format(CultureInfo.InvariantCulture, Definition.Progress, Number(Value));

    // Rounded down, so the text never claims a threshold that has not been reached.
    private static string Number(double value) =>
        (Math.Floor(value * 10 + 1e-9) / 10).ToString("0.#", CultureInfo.InvariantCulture);
}

public static class TrophyCatalog
{
    private const double Hour = 3600;
    private const double MinimumSeconds = 60;
    private static readonly int[] NightHours = [23, 0, 1, 2, 3];
    private static readonly int[] EarlyHours = [4, 5, 6];

    private static readonly (TrophyDefinition Definition, Func<TrophyInput, double> Value)[] Trophies =
    [
        (new("marathoner", "Marathoner", "\uE7F6", [10, 100, 500],
                "Listen for {0} hours in total.", "You have listened for {0} hours."),
            input => input.Data.Slices.Sum(s => s.ListenedSeconds) / Hour),
        (new("time-bender", "Time Bender", "\uE916", [1, 24, 100],
                "Save {0} hours by listening faster.", "You have saved {0} hours."),
            input => input.Data.Slices.Sum(s => s.SavedSeconds) / Hour),
        (new("finisher", "Finisher", "\uE73E", [1, 10, 50],
                "Finish {0} audiobooks.", "You have finished {0} audiobooks."),
            input => input.Data.Completions.Count),
        (new("collector", "Collector", "\uE8F1", [10, 50, 200],
                "Grow your library to {0} titles.", "Your library has {0} titles."),
            input => input.LibraryCount),
        (new("daily-habit", "Daily Habit", "\uE787", [3, 7, 30],
                "Listen {0} days in a row.", "Your longest streak is {0} days."),
            input => LongestStreak(ListeningDays(input.Data))),
        (new("night-owl", "Night Owl", "\uE708", [1, 10, 50],
                "Listen for {0} hours between 11 pm and 4 am.", "You have listened {0} hours at night."),
            input => input.Data.Slices.Where(s => NightHours.Contains(s.Hour)).Sum(s => s.ListenedSeconds) / Hour),
        (new("early-bird", "Early Bird", "\uE706", [1, 10, 50],
                "Listen for {0} hours between 4 am and 7 am.", "You have listened {0} hours in the early morning."),
            input => input.Data.Slices.Where(s => EarlyHours.Contains(s.Hour)).Sum(s => s.ListenedSeconds) / Hour),
        (new("weekend-warrior", "Weekend Warrior", "\uE7FC", [2, 5, 10],
                "Listen for {0} hours on a single weekend.", "Your best weekend is {0} hours."),
            input => Best(input.Data.Slices
                .Where(s => s.Day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                .GroupBy(s => s.Day.DayOfWeek == DayOfWeek.Sunday ? s.Day.AddDays(-1) : s.Day)
                .Select(weekend => weekend.Sum(s => s.ListenedSeconds))) / Hour),
        (new("binge-day", "Binge Day", "\uE945", [2, 5, 8],
                "Listen for {0} hours in a single day.", "Your best day is {0} hours."),
            input => Best(input.Data.Slices.GroupBy(s => s.Day).Select(day => day.Sum(s => s.ListenedSeconds))) / Hour),
        (new("speed-reader", "Speed Reader", "\uEC4A", [1, 10, 50],
                "Listen for {0} hours at double speed or faster.", "You have listened {0} hours at 2× or faster."),
            input => input.Data.Slices.Where(s => s.ListenedSeconds > 0 && s.SavedSeconds >= s.ListenedSeconds)
                .Sum(s => s.ListenedSeconds) / Hour),
        (new("sprint", "Sprint", "\uE7C1", [2, 3, 5],
                "Finish {0} audiobooks within 7 days.", "Your best week is {0} audiobooks."),
            input => Best(input.Data.Completions.Select(start => (double)input.Data.Completions.Count(other =>
                other.CompletedAt >= start.CompletedAt && other.CompletedAt < start.CompletedAt.AddDays(7))))),
        (new("juggler", "Juggler", "\uE8EE", [2, 3, 5],
                "Listen to {0} different audiobooks on one day.", "Your best is {0} audiobooks on one day."),
            input => Best(input.Data.Slices.GroupBy(s => s.Day).Select(day => (double)day
                .GroupBy(s => s.AudiobookId).Count(book => book.Sum(s => s.ListenedSeconds) >= MinimumSeconds)))),
        (new("regular", "Regular", "\uE8D1", [10, 20, 28],
                "Listen on {0} days of one month.", "Your best month has {0} listening days."),
            input => Best(ListeningDays(input.Data).GroupBy(day => (day.Year, day.Month))
                .Select(month => (double)month.Count()))),
        (new("around-the-clock", "Around the Clock", "\uE823", [12, 18, 24],
                "Listen during {0} different hours of the day.", "You have listened during {0} different hours."),
            input => input.Data.Slices.GroupBy(s => s.Hour).Count(hour => hour.Sum(s => s.ListenedSeconds) >= MinimumSeconds))
    ];

    public static IReadOnlyList<TrophyDefinition> All { get; } = Trophies.Select(trophy => trophy.Definition).ToList();

    public static IReadOnlyList<TrophyState> Evaluate(TrophyInput input) =>
        Trophies.Select(trophy =>
        {
            var value = trophy.Value(input);
            if (!double.IsFinite(value) || value < 0) value = 0;
            return new TrophyState(trophy.Definition, value, trophy.Definition.Thresholds.Count(t => value >= t - 1e-9));
        }).ToList();

    public static string TierName(int tier) => tier switch { 1 => "Bronze", 2 => "Silver", >= 3 => "Gold", _ => "Locked" };

    private static List<DateOnly> ListeningDays(StatsData data) =>
        data.Slices.Where(s => s.ListenedSeconds > 0).Select(s => s.Day).Distinct().OrderBy(day => day).ToList();

    private static double LongestStreak(List<DateOnly> days)
    {
        int best = 0, run = 0;
        for (var i = 0; i < days.Count; i++)
        {
            run = i > 0 && days[i] == days[i - 1].AddDays(1) ? run + 1 : 1;
            best = Math.Max(best, run);
        }

        return best;
    }

    private static double Best(IEnumerable<double> values) => values.DefaultIfEmpty(0).Max();
}
