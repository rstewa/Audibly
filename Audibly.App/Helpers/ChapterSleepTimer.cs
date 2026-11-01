using System;
using System.Collections.Generic;

namespace Audibly.App.Helpers;

/// <summary>Tracks a chapter boundary on the original audio timeline, independent of playback speed.</summary>
public sealed class ChapterSleepTimer(IReadOnlyList<double> sourceDurationsSeconds, int sourceFileIndex, double endPositionMs)
{
    public bool HasReachedEnd(int currentSourceFileIndex, double currentPositionMs) =>
        currentSourceFileIndex > sourceFileIndex ||
        currentSourceFileIndex == sourceFileIndex && currentPositionMs >= endPositionMs;

    public TimeSpan GetRemainingTime(int currentSourceFileIndex, double currentPositionMs, double playbackSpeed)
    {
        if (HasReachedEnd(currentSourceFileIndex, currentPositionMs)) return TimeSpan.Zero;

        var remainingMs = endPositionMs - currentPositionMs;
        for (var i = currentSourceFileIndex; i < sourceFileIndex; i++)
            remainingMs += sourceDurationsSeconds[i] * 1000;

        return TimeSpan.FromMilliseconds(Math.Max(0, remainingMs) / playbackSpeed);
    }
}
