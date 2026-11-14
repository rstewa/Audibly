using Audibly.App.Helpers;
using Xunit;

namespace Audibly.Audio.Tests;

public class PlaybackSpeedPresetsTests
{
    [Theory]
    [InlineData(2.7, 2.7)]
    [InlineData(2.75, 2.75)]
    [InlineData(2.72, 2.7)]
    [InlineData(2.73, 2.75)]
    [InlineData(0.1, 0.5)]
    [InlineData(9, 4)]
    [InlineData(double.NaN, 1)]
    public void SnapKeepsFineStepsAndClamps(double speed, double expected) =>
        Assert.Equal(expected, PlaybackSpeedPresets.Snap(speed));

    [Fact]
    public void EverySliderStepRoundTripsToTheSameSpeed()
    {
        for (var index = 0; index <= PlaybackSpeedPresets.StepCount; index++)
        {
            var speed = PlaybackSpeedPresets.FromIndex(index);
            Assert.Equal(index, PlaybackSpeedPresets.ToIndex(speed));
            Assert.Equal(speed, PlaybackSpeedPresets.Snap(speed));
        }

        Assert.Equal(0.5, PlaybackSpeedPresets.FromIndex(0));
        Assert.Equal(4.0, PlaybackSpeedPresets.FromIndex(PlaybackSpeedPresets.StepCount));
        Assert.Equal(2.7, PlaybackSpeedPresets.FromIndex(PlaybackSpeedPresets.ToIndex(2.7)));
    }

    [Fact]
    public void QuickPicksAreValidSpeeds()
    {
        foreach (var speed in PlaybackSpeedPresets.QuickPicks)
            Assert.Equal(speed, PlaybackSpeedPresets.Snap(speed));
    }
}
