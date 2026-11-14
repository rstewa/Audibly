using System;
using System.Collections.Generic;
using System.Linq;

namespace Audibly.App.Helpers;

public static class PlaybackTimeRemaining
{
    public static string Format(double durationMs, double positionMs, double playbackSpeed)
    {
        if (!double.IsFinite(durationMs) || !double.IsFinite(positionMs)) return "0:00";
        if (!double.IsFinite(playbackSpeed) || playbackSpeed <= 0) playbackSpeed = 1;

        var seconds = (long)Math.Ceiling(Math.Max(0, durationMs - Math.Max(0, positionMs)) / 1000 / playbackSpeed);
        var hours = seconds / 3600;
        return hours > 0
            ? $"{hours}:{seconds / 60 % 60:00}:{seconds % 60:00}"
            : $"{seconds / 60}:{seconds % 60:00}";
    }

    public static string ForBook(double durationSeconds, IEnumerable<double> sourceDurationsSeconds,
        int currentSourceFileIndex, double currentPositionMs, double playbackSpeed)
    {
        var previousFilesMs = sourceDurationsSeconds.Take(currentSourceFileIndex).Sum() * 1000;
        return Format(durationSeconds * 1000, previousFilesMs + Math.Max(0, currentPositionMs), playbackSpeed);
    }
}
