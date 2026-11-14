using Audibly.App.Helpers;
using Xunit;

namespace Audibly.Audio.Tests;

public class PlaybackTimeRemainingTests
{
    [Theory]
    [InlineData(300000, 120000, 1.25, "2:24")]
    [InlineData(300000, 60000, 2, "2:00")]
    [InlineData(300000, 300001, 1, "0:00")]
    [InlineData(300000, 299900, 4, "0:01")]
    [InlineData(360000000, 0, 1, "100:00:00")]
    public void ChapterTimeReflectsSpeedAndSeekPosition(double durationMs, double positionMs, double speed, string expected)
    {
        Assert.Equal(expected, PlaybackTimeRemaining.Format(durationMs, positionMs, speed));
    }

    [Fact]
    public void BookTimeIncludesAllFilesAfterSeeking()
    {
        double[] durations = [120, 180, 240];
        Assert.Equal("3:00", PlaybackTimeRemaining.ForBook(540, durations, 1, 60000, 2));
        Assert.Equal("1:20", PlaybackTimeRemaining.ForBook(540, durations, 2, 120000, 1.5));
    }

    [Fact]
    public void BookTimeStaysContinuousAcrossFileBoundary()
    {
        double[] durations = [120, 180, 240];
        Assert.Equal("3:30", PlaybackTimeRemaining.ForBook(540, durations, 0, 120000, 2));
        Assert.Equal("3:30", PlaybackTimeRemaining.ForBook(540, durations, 1, 0, 2));
    }
}
