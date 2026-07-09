using System;
using System.Collections.Generic;

namespace Audibly.App.Services.Transcription;

/// <summary>
///     Merges the words of two consecutive, overlapping transcription windows.
///     Both windows transcribe the shared overlap region independently; the merge finds
///     the longest run of matching words (same normalized text, start times within
///     tolerance) inside the overlap and cuts mid-run — the previous window contributes
///     the words before the cut, the new window the words after. When nothing matches
///     (music, noise), it falls back to cutting both at the largest silence near the
///     overlap midpoint.
/// </summary>
public static class OverlapStitcher
{
    private const int MatchToleranceMs = 400;
    private const int MinMatchRun = 2;

    /// <summary>
    ///     <paramref name="previousTail" /> — words of the previous window inside the overlap
    ///     (start ≥ overlapStartMs). <paramref name="currentWindow" /> — all words of the new
    ///     window. Returns the merged replacement for both.
    /// </summary>
    public static List<TimedWord> Merge(IReadOnlyList<TimedWord> previousTail, IReadOnlyList<TimedWord> currentWindow,
        long overlapStartMs, long overlapEndMs)
    {
        if (previousTail.Count == 0) return new List<TimedWord>(currentWindow);

        // words of the new window inside the overlap region
        var currentOverlapCount = 0;
        while (currentOverlapCount < currentWindow.Count &&
               currentWindow[currentOverlapCount].StartMs < overlapEndMs)
            currentOverlapCount++;

        if (currentOverlapCount == 0)
        {
            // the new window heard nothing in the overlap — keep the previous words
            var result = new List<TimedWord>(previousTail);
            result.AddRange(currentWindow);
            return result;
        }

        var (previousCut, currentCut) = FindCut(previousTail, currentWindow, currentOverlapCount, overlapStartMs,
            overlapEndMs);

        var merged = new List<TimedWord>(previousCut + (currentWindow.Count - currentCut));
        for (var i = 0; i < previousCut; i++) merged.Add(previousTail[i]);
        for (var i = currentCut; i < currentWindow.Count; i++)
        {
            // guard against overlap-region words re-introducing out-of-order times
            if (merged.Count > 0 && currentWindow[i].StartMs < merged[^1].StartMs) continue;
            merged.Add(currentWindow[i]);
        }

        return merged;
    }

    private static (int PreviousCut, int CurrentCut) FindCut(IReadOnlyList<TimedWord> previousTail,
        IReadOnlyList<TimedWord> currentWindow, int currentOverlapCount, long overlapStartMs, long overlapEndMs)
    {
        // longest run of matching words between the two transcriptions of the overlap
        var bestRun = 0;
        var bestPrevious = -1;
        var bestCurrent = -1;

        for (var p = 0; p < previousTail.Count; p++)
        for (var c = 0; c < currentOverlapCount; c++)
        {
            var run = 0;
            while (p + run < previousTail.Count && c + run < currentOverlapCount &&
                   WordsMatch(previousTail[p + run], currentWindow[c + run]))
                run++;

            if (run > bestRun)
            {
                bestRun = run;
                bestPrevious = p;
                bestCurrent = c;
            }
        }

        if (bestRun >= MinMatchRun)
        {
            // cut in the middle of the matching run
            var half = bestRun / 2;
            return (bestPrevious + half, bestCurrent + half);
        }

        // fallback: cut both transcriptions at the largest silence near the overlap midpoint
        var midpointMs = (overlapStartMs + overlapEndMs) / 2;
        return (CutAtSilence(previousTail, previousTail.Count, midpointMs),
            CutAtSilence(currentWindow, currentOverlapCount, midpointMs));
    }

    private static bool WordsMatch(TimedWord a, TimedWord b)
    {
        return Math.Abs(a.StartMs - b.StartMs) <= MatchToleranceMs &&
               string.Equals(Normalize(a.Text), Normalize(b.Text), StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string text)
    {
        return text.Trim().TrimEnd('.', ',', '!', '?', ';', ':', '…', '"', '\'', ')', ']');
    }

    /// <summary>
    ///     Index of the first word at/after the largest inter-word gap within ±1 s of the
    ///     midpoint; falls back to the first word starting after the midpoint.
    /// </summary>
    private static int CutAtSilence(IReadOnlyList<TimedWord> words, int limit, long midpointMs)
    {
        var bestIndex = -1;
        long bestGap = -1;

        for (var i = 1; i < limit; i++)
        {
            var gapCenter = (words[i - 1].EndMs + (long)words[i].StartMs) / 2;
            if (Math.Abs(gapCenter - midpointMs) > 1000) continue;

            var gap = words[i].StartMs - (long)words[i - 1].EndMs;
            if (gap > bestGap)
            {
                bestGap = gap;
                bestIndex = i;
            }
        }

        if (bestIndex >= 0) return bestIndex;

        for (var i = 0; i < limit; i++)
            if (words[i].StartMs >= midpointMs)
                return i;

        return limit;
    }
}
