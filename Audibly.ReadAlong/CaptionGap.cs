namespace Audibly.ReadAlong;

public static class CaptionGap
{
    // UI messaging only: never extend or invent a word's actual highlight range.
    // Media time, rather than wall time, keeps a paused inter-word gap stable.
    public static bool IsSignificant(int previousSource, double previousPosition, double? lastMatchedEnd,
        int source, double position, double graceSeconds = 1.5)
    {
        if (source != previousSource || Math.Abs(position - previousPosition) > 2) return true;
        return lastMatchedEnd is null || position - lastMatchedEnd.Value >= graceSeconds;
    }
}
