using Audibly.App.Helpers;
using Xunit;

namespace Audibly.Audio.Tests;

public class ChapterSleepTimerTests
{
    [Theory]
    [InlineData(1, 60)]
    [InlineData(1.25, 48)]
    [InlineData(1.5, 40)]
    [InlineData(2, 30)]
    [InlineData(4, 15)]
    public void CountdownUsesCurrentSpeed(double speed, double secondsLeft)
    {
        var timer = new ChapterSleepTimer([180], 0, 120000);
        Assert.Equal(secondsLeft, timer.GetRemainingTime(0, 60000, speed).TotalSeconds);
        Assert.False(timer.HasReachedEnd(0, 60000));
    }

    [Fact]
    public void SpeedChangesAndSeekingKeepTheOriginalChapterBoundary()
    {
        var timer = new ChapterSleepTimer([180], 0, 120000);
        Assert.Equal(60, timer.GetRemainingTime(0, 60000, 1).TotalSeconds);
        Assert.Equal(15, timer.GetRemainingTime(0, 60000, 4).TotalSeconds);
        Assert.Equal(50, timer.GetRemainingTime(0, 20000, 2).TotalSeconds);
        Assert.False(timer.HasReachedEnd(0, 119999));
        Assert.True(timer.HasReachedEnd(0, 120000));
        Assert.Equal(TimeSpan.Zero, timer.GetRemainingTime(0, 120001, 4));
    }

    [Fact]
    public void SeekingToEarlierFileIncludesTimeUntilTargetChapter()
    {
        var timer = new ChapterSleepTimer([120, 180, 240], 1, 90000);
        Assert.Equal(75, timer.GetRemainingTime(0, 60000, 2).TotalSeconds);
        Assert.False(timer.HasReachedEnd(0, double.PositiveInfinity));
        Assert.True(timer.HasReachedEnd(1, double.PositiveInfinity));
        Assert.True(timer.HasReachedEnd(2, 0));
    }
}
