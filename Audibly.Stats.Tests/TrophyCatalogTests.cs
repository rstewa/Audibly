using Audibly.Stats;

namespace Audibly.Stats.Tests;

public class TrophyCatalogTests
{
    private const double H = 3600;
    private static readonly Guid Book = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static ListeningSlice S(int day, int hour, double listened, double saved = 0, Guid? book = null) =>
        new(new DateOnly(2026, 11, day), hour, book ?? Book, listened, saved);

    private static TrophyState Get(string id, IEnumerable<ListeningSlice>? slices = null,
        IEnumerable<Completion>? completions = null, int library = 0) =>
        TrophyCatalog.Evaluate(new TrophyInput(new StatsData((slices ?? []).ToList(), (completions ?? []).ToList()), library))
            .Single(state => state.Definition.Id == id);

    [Fact]
    public void CatalogHasFourteenTrophiesWithUniqueIdsAndThreeAscendingThresholds()
    {
        Assert.Equal(14, TrophyCatalog.All.Count);
        Assert.Equal(14, TrophyCatalog.All.Select(t => t.Id).Distinct().Count());
        Assert.Equal(14, TrophyCatalog.All.Select(t => t.Name).Distinct().Count());
        Assert.All(TrophyCatalog.All, t =>
        {
            Assert.Equal(3, t.Thresholds.Length);
            Assert.True(0 < t.Thresholds[0] && t.Thresholds[0] < t.Thresholds[1] && t.Thresholds[1] < t.Thresholds[2]);
            Assert.Contains("{0}", t.Goal);
            Assert.Contains("{0}", t.Progress);
            Assert.False(string.IsNullOrWhiteSpace(t.Glyph));
        });
    }

    [Fact]
    public void EvaluateReturnsEveryTrophyInCatalogOrder() =>
        Assert.Equal(TrophyCatalog.All.Select(t => t.Id),
            TrophyCatalog.Evaluate(new TrophyInput(StatsData.Empty, 0)).Select(s => s.Definition.Id));

    [Fact]
    public void EmptyDataLeavesEverythingLocked() =>
        Assert.All(TrophyCatalog.Evaluate(new TrophyInput(StatsData.Empty, 0)), state =>
        {
            Assert.Equal(0, state.Tier);
            Assert.Equal(0, state.Value);
            Assert.Equal(state.Definition.Thresholds[0], state.NextThreshold);
        });

    [Fact]
    public void TierIsTheNumberOfThresholdsReached()
    {
        var state = Get("marathoner", [S(1, 10, 60 * H), S(2, 10, 40 * H)]);
        Assert.Equal(2, state.Tier);
        Assert.Equal(100, state.Value, 6);
        Assert.Equal(500, state.NextThreshold);
    }

    [Fact]
    public void GoldHasNoNextThreshold()
    {
        var state = Get("marathoner", [S(1, 10, 500 * H)]);
        Assert.Equal(3, state.Tier);
        Assert.Null(state.NextThreshold);
        Assert.Equal("All tiers unlocked.", state.GoalText);
    }

    [Fact]
    public void TimeBenderCountsSavedHours() => Assert.Equal(1, Get("time-bender", [S(1, 10, H, H)]).Tier);

    [Fact]
    public void CollectorUsesTheLibraryCount() => Assert.Equal(2, Get("collector", library: 50).Tier);

    [Fact]
    public void FinisherCountsCompletions() =>
        Assert.Equal(1, Get("finisher", completions: [new Completion(Book, new DateTime(2026, 11, 1))]).Tier);

    [Fact]
    public void DailyHabitNeedsConsecutiveDays() =>
        Assert.Equal(3, Get("daily-habit", [S(1, 10, 60), S(2, 10, 60), S(3, 10, 60), S(5, 10, 60), S(6, 10, 60)]).Value);

    [Fact]
    public void NightOwlOnlyCountsNightHours() =>
        Assert.Equal(1, Get("night-owl", [S(1, 23, H), S(1, 12, H), S(1, 4, H)]).Value, 6);

    [Fact]
    public void EarlyBirdCountsFourToSeven() =>
        Assert.Equal(2, Get("early-bird", [S(1, 4, H), S(1, 6, H), S(1, 7, H), S(1, 3, H)]).Value, 6);

    [Fact]
    public void WeekendWarriorAddsSaturdayAndSunday() =>
        Assert.Equal(2.5, Get("weekend-warrior", [S(14, 10, 1.5 * H), S(15, 10, H), S(13, 10, 9 * H), S(21, 10, H)]).Value, 6);

    [Fact]
    public void BingeDayUsesTheBestSingleDay() =>
        Assert.Equal(3, Get("binge-day", [S(1, 10, 2 * H), S(1, 20, H), S(2, 10, 2.5 * H)]).Value, 6);

    [Fact]
    public void SpeedReaderCountsSlicesAtDoubleSpeedOrFaster() =>
        Assert.Equal(1, Get("speed-reader", [S(1, 10, H, H), S(1, 11, H, H / 2)]).Value, 6);

    [Fact]
    public void SprintUsesASlidingSevenDayWindow()
    {
        DateTime D(int day) => new(2026, 11, day, 12, 0, 0);
        var completions = new[] { 1, 5, 7, 20 }.Select(day => new Completion(Guid.NewGuid(), D(day)));
        Assert.Equal(3, Get("sprint", completions: completions).Value);
        Assert.Equal(1, Get("sprint", completions: [new Completion(Book, D(1)), new Completion(Book, D(8))]).Value);
    }

    [Fact]
    public void SprintCountsABookFinishedTwiceOnce()
    {
        DateTime D(int day) => new(2026, 11, day, 12, 0, 0);
        var other = Guid.NewGuid();
        Assert.Equal(2, Get("sprint", completions:
            [new Completion(Book, D(1)), new Completion(Book, D(3)), new Completion(other, D(4)), new Completion(Book, D(6))]).Value);
    }

    [Fact]
    public void JugglerIgnoresBooksUnderAMinute() =>
        Assert.Equal(2, Get("juggler", [S(1, 10, 120, book: Guid.NewGuid()), S(1, 11, 30, book: Guid.NewGuid()),
            S(1, 12, 40, book: Book), S(1, 13, 40, book: Book)]).Value);

    [Fact]
    public void RegularCountsListeningDaysOfTheBestMonth()
    {
        var november = Enumerable.Range(1, 12).Select(day => S(day, 10, 60));
        var october = Enumerable.Range(1, 3).Select(day => new ListeningSlice(new DateOnly(2026, 10, day), 10, Book, 60, 0));
        var state = Get("regular", november.Concat(october));
        Assert.Equal(12, state.Value);
        Assert.Equal(1, state.Tier);
    }

    [Fact]
    public void AroundTheClockCountsDistinctHours() =>
        Assert.Equal(3, Get("around-the-clock", [S(1, 8, 60), S(2, 8, 600), S(1, 9, 30), S(2, 9, 30), S(1, 22, 61), S(1, 23, 59)]).Value);

    [Fact]
    public void TextsContainTheNumbers()
    {
        var state = Get("marathoner", [S(1, 10, 12.5 * H)]);
        Assert.Equal("Listen for 100 hours in total.", state.GoalText);
        Assert.Equal("You have listened for 12.5 hours.", state.ProgressText);
        Assert.Equal("Your library has 3 titles.", Get("collector", library: 3).ProgressText);
    }

    [Fact]
    public void ProgressTextNeverRoundsUpToTheThreshold() =>
        Assert.Equal("You have listened for 9.9 hours.", Get("marathoner", [S(1, 10, 9.99 * H)]).ProgressText);

    [Fact]
    public void TierNames() =>
        Assert.Equal(["Locked", "Bronze", "Silver", "Gold"], Enumerable.Range(0, 4).Select(TrophyCatalog.TierName));
}
