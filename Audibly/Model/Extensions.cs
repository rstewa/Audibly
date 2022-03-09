using System;
using FlyleafLib.MediaFramework.MediaDemuxer;

namespace Audibly.Model;

public static class Extensions
{
    public static bool InRange(this Demuxer.Chapter chapter, long curTime)
    {
        return curTime >= chapter.StartTime && curTime < chapter.EndTime;
    }

    public static long ToMs(this long ticks)
    {
        return ticks / TimeSpan.TicksPerMillisecond;
    }

    public static long ToTicks(this long ms)
    {
        return TimeSpan.FromMilliseconds(ms).Ticks;
    }
}