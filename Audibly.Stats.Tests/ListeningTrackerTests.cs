using Audibly.Stats;

namespace Audibly.Stats.Tests;

public class ListeningTrackerTests
{
    private static readonly Guid Book = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Other = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static DateTime T(int h, int m, int s) => new(2026, 11, 14, h, m, s);

    [Fact]
    public void CountsRealTimeAndSavedTimeAtDoubleSpeed()
    {
        var t = new ListeningTracker();
        t.Observe(Book, 0, 100, 2.0, T(10, 0, 0));
        t.Observe(Book, 0, 102, 2.0, T(10, 0, 1));
        var s = Assert.Single(t.Drain());
        Assert.Equal(1.0, s.ListenedSeconds, 3);
        Assert.Equal(1.0, s.SavedSeconds, 3);
        Assert.Equal(new DateOnly(2026, 11, 14), s.Day);
        Assert.Equal(10, s.Hour);
        Assert.Equal(Book, s.AudiobookId);
    }

    [Fact]
    public void SlowerThanNormalSavesNothing()
    {
        var t = new ListeningTracker();
        t.Observe(Book, 0, 100, 0.5, T(10, 0, 0));
        t.Observe(Book, 0, 100.5, 0.5, T(10, 0, 1));
        var s = Assert.Single(t.Drain());
        Assert.Equal(1.0, s.ListenedSeconds, 3);
        Assert.Equal(0.0, s.SavedSeconds, 3);
    }

    [Fact]
    public void SeekForwardIsNotListening()
    {
        var t = new ListeningTracker();
        t.Observe(Book, 0, 100, 1.0, T(10, 0, 0));
        t.Observe(Book, 0, 400, 1.0, T(10, 0, 1));
        Assert.Empty(t.Drain());
        t.Observe(Book, 0, 401, 1.0, T(10, 0, 2));
        Assert.Equal(1.0, Assert.Single(t.Drain()).ListenedSeconds, 3);
    }

    [Fact]
    public void SeekBackwardIsNotListening()
    {
        var t = new ListeningTracker();
        t.Observe(Book, 0, 400, 1.0, T(10, 0, 0));
        t.Observe(Book, 0, 100, 1.0, T(10, 0, 1));
        Assert.Empty(t.Drain());
    }

    [Fact]
    public void NextFileRestartingAtZeroKeepsCounting()
    {
        var t = new ListeningTracker();
        t.Observe(Book, 0, 599, 1.0, T(10, 0, 0));
        t.Observe(Book, 1, 0.2, 1.0, T(10, 0, 1));
        Assert.Empty(t.Drain());
        t.Observe(Book, 1, 1.2, 1.0, T(10, 0, 2));
        Assert.Equal(1.0, Assert.Single(t.Drain()).ListenedSeconds, 3);
    }

    [Fact]
    public void OtherBookDoesNotCountTheFirstTick()
    {
        var t = new ListeningTracker();
        t.Observe(Book, 0, 100, 1.0, T(10, 0, 0));
        t.Observe(Other, 0, 101, 1.0, T(10, 0, 1));
        Assert.Empty(t.Drain());
    }

    [Fact]
    public void BreakForgetsTheLastPosition()
    {
        var t = new ListeningTracker();
        t.Observe(Book, 0, 100, 1.0, T(10, 0, 0));
        t.Break();
        t.Observe(Book, 0, 101, 1.0, T(10, 0, 1));
        Assert.Empty(t.Drain());
    }

    [Fact]
    public void TickAfterMidnightCountsForTheNewDayAndHour()
    {
        var t = new ListeningTracker();
        t.Observe(Book, 0, 100, 1.0, T(23, 59, 58));
        t.Observe(Book, 0, 101, 1.0, T(23, 59, 59));
        t.Observe(Book, 0, 103, 1.0, T(0, 0, 1).AddDays(1));
        var slices = t.Drain().OrderBy(s => s.Day).ToList();
        Assert.Equal(2, slices.Count);
        Assert.Equal((new DateOnly(2026, 11, 14), 23), (slices[0].Day, slices[0].Hour));
        Assert.Equal(1.0, slices[0].ListenedSeconds, 3);
        Assert.Equal((new DateOnly(2026, 11, 15), 0), (slices[1].Day, slices[1].Hour));
        Assert.Equal(2.0, slices[1].ListenedSeconds, 3);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void IgnoresInvalidSpeed(double speed)
    {
        var t = new ListeningTracker();
        t.Observe(Book, 0, 100, speed, T(10, 0, 0));
        t.Observe(Book, 0, 101, speed, T(10, 0, 1));
        Assert.Empty(t.Drain());
    }

    [Fact]
    public void DrainEmptiesButKeepsPosition()
    {
        var t = new ListeningTracker();
        t.Observe(Book, 0, 100, 1.0, T(10, 0, 0));
        t.Observe(Book, 0, 101, 1.0, T(10, 0, 1));
        Assert.Single(t.Drain());
        Assert.Empty(t.Drain());
        t.Observe(Book, 0, 102, 1.0, T(10, 0, 2));
        Assert.Equal(1.0, Assert.Single(t.Drain()).ListenedSeconds, 3);
    }
}
