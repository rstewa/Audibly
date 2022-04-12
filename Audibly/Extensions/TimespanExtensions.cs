using System;

namespace Audibly.Extensions;

public static class TimespanExtensions
{
    public static long ToMs(this long ticks)
    {
        return ticks / TimeSpan.TicksPerMillisecond;
    }

    public static long ToTicks(this long ms)
    {
        return TimeSpan.FromMilliseconds(ms).Ticks;
    }

    public static string ToStr_ms(this long ms)
    {
        var t = TimeSpan.FromMilliseconds(ms);
        return $@"{(int)t.TotalHours}:{t:mm}:{t:ss}";
    }

    public static string ToStr_ms(this int ms)
    {
        var t = TimeSpan.FromMilliseconds(ms);
        return $@"{t.TotalHours}:{t:mm}:{t:ss}";
    }
}