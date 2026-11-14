using Audibly.Stats;

namespace Audibly.Stats.Tests;

public class StatsSummaryTests
{
    private static readonly Guid Book = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Other = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static ListeningSlice S(int year, int month, int day, double listened, double saved = 0, Guid? book = null, int hour = 10) =>
        new(new DateOnly(year, month, day), hour, book ?? Book, listened, saved);

    private static StatsData Data(params ListeningSlice[] slices) => new(slices, []);

    [Fact]
    public void TotalSumsListenedAndSavedOverAllBooks()
    {
        var total = StatsSummary.Total(Data(S(2026, 11, 14, 100, 50), S(2026, 11, 14, 200, 25, Other), S(2026, 11, 13, 10)));
        Assert.Equal(new Totals(310, 75), total);
    }

    [Fact]
    public void ForBookOnlyCountsThatBook()
    {
        var data = Data(S(2026, 11, 14, 100, 50), S(2026, 11, 14, 200, 25, Other), S(2026, 11, 13, 10, 5));
        Assert.Equal(new Totals(110, 55), StatsSummary.ForBook(data, Book));
    }

    [Fact]
    public void ForUnknownBookIsZero() =>
        Assert.Equal(new Totals(0, 0), StatsSummary.ForBook(Data(S(2026, 11, 14, 100)), Guid.NewGuid()));

    [Fact]
    public void LastListenedDaysSkipsDaysWithoutListening()
    {
        var data = Data(S(2026, 11, 1, 60), S(2026, 11, 3, 60), S(2026, 11, 4, 60), S(2026, 11, 9, 60),
            S(2026, 11, 10, 60), S(2026, 11, 14, 60, hour: 9), S(2026, 11, 14, 30, hour: 20, book: Other));
        var days = StatsSummary.LastListenedDays(data);
        Assert.Equal(["Tue 3", "Wed 4", "Mon 9", "Tue 10", "Sat 14"], days.Select(d => d.Label));
        Assert.Equal(90, days[^1].ListenedSeconds);
        Assert.Equal(new DateOnly(2026, 11, 14), days[^1].Start);
    }

    [Fact]
    public void LastListenedDaysWithFewerDaysReturnsWhatExists() =>
        Assert.Equal(["Fri 13", "Sat 14"],
            StatsSummary.LastListenedDays(Data(S(2026, 11, 14, 60), S(2026, 11, 13, 60))).Select(d => d.Label));

    [Fact]
    public void MonthsFillGapsWithZero()
    {
        var months = StatsSummary.Months(Data(S(2026, 6, 5, 100), S(2026, 6, 20, 50), S(2026, 9, 1, 30)));
        Assert.Equal(["Jun", "Jul", "Aug", "Sep"], months.Select(m => m.Label));
        Assert.Equal([150, 0, 0, 30], months.Select(m => m.ListenedSeconds));
        Assert.Equal(new DateOnly(2026, 7, 1), months[1].Start);
    }

    [Fact]
    public void MonthsAcrossYearsCarryTheYear() =>
        Assert.Equal(["Dec 25", "Jan 26"],
            StatsSummary.Months(Data(S(2025, 12, 31, 10), S(2026, 1, 1, 10))).Select(m => m.Label));

    [Fact]
    public void YearsListEveryYearWithData()
    {
        var years = StatsSummary.Years(Data(S(2024, 3, 1, 10), S(2026, 1, 1, 20), S(2026, 5, 1, 5)));
        Assert.Equal(["2024", "2025", "2026"], years.Select(y => y.Label));
        Assert.Equal([10, 0, 25], years.Select(y => y.ListenedSeconds));
    }

    [Fact]
    public void DeletedBookStillCountsInTotals()
    {
        var gone = Guid.NewGuid();
        var data = Data(S(2026, 11, 14, 100, 10, gone));
        Assert.Equal(new Totals(100, 10), StatsSummary.Total(data));
        Assert.Equal(100, Assert.Single(StatsSummary.Months(data)).ListenedSeconds);
        Assert.Equal(100, Assert.Single(StatsSummary.Years(data)).ListenedSeconds);
        Assert.Equal(100, Assert.Single(StatsSummary.LastListenedDays(data)).ListenedSeconds);
    }

    [Fact]
    public void EmptyDataGivesEmptyListsAndZeroTotals()
    {
        Assert.Equal(new Totals(0, 0), StatsSummary.Total(StatsData.Empty));
        Assert.Empty(StatsSummary.LastListenedDays(StatsData.Empty));
        Assert.Empty(StatsSummary.Months(StatsData.Empty));
        Assert.Empty(StatsSummary.Years(StatsData.Empty));
        Assert.Equal(0, StatsSummary.Average([]));
    }

    [Fact]
    public void AverageIncludesEmptyPeriods() =>
        Assert.Equal(45, StatsSummary.Average(StatsSummary.Months(Data(S(2026, 6, 5, 150), S(2026, 9, 1, 30)))));
}

public class DurationFormatTests
{
    [Theory]
    [InlineData(18240, "5h 4m")]
    [InlineData(2580, "43m")]
    [InlineData(59, "0m")]
    [InlineData(3600, "1h 0m")]
    [InlineData(3659.9, "1h 0m")]
    [InlineData(-5, "0m")]
    [InlineData(double.NaN, "0m")]
    [InlineData(double.PositiveInfinity, "0m")]
    [InlineData(1800000, "500h 0m")]
    public void Short(double seconds, string expected) => Assert.Equal(expected, DurationFormat.Short(seconds));
}

public class ChartAxisTests
{
    [Fact]
    public void ElevenAndAHalfHoursGetsATwelveHourAxis()
    {
        var axis = ChartAxis.For(11.5 * 3600);
        Assert.Equal(43200, axis.MaxSeconds);
        Assert.Equal([0, 10800, 21600, 32400, 43200], axis.TickSeconds);
    }

    [Fact]
    public void FortyFiveMinutesGetsAOneHourAxis() => Assert.Equal(3600, ChartAxis.For(45 * 60).MaxSeconds);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    public void ZeroGetsTheSmallestUsefulAxis(double value) => Assert.Equal(3600, ChartAxis.For(value).MaxSeconds);

    [Fact]
    public void ExactFitDoesNotGrowTheAxis() => Assert.Equal(43200, ChartAxis.For(43200).MaxSeconds);

    [Fact]
    public void HugeValuesStillFit() => Assert.True(ChartAxis.For(9000 * 3600.0).MaxSeconds >= 9000 * 3600.0);
}
