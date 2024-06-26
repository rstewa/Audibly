﻿// Author: rstewa · https://github.com/rstewa
// Created: 3/21/2024
// Updated: 3/22/2024

using System;

namespace Audibly.App.Extensions;

public static class TimespanExtensions
{
    public static long ToMs(this long ticks)
    {
        return ticks / TimeSpan.TicksPerMillisecond;
    }

    public static long ToMs(this int ticks)
    {
        return ticks * 1000;
    }

    public static long ToTicks(this long ms)
    {
        return TimeSpan.FromMilliseconds(ms).Ticks;
    }

    public static string ToStr_ms(this double ms)
    {
        var t = TimeSpan.FromMilliseconds(ms);
        return $@"{(int)t.TotalHours}:{t:mm}:{t:ss}";
    }

    public static string ToStr_ms(this long ms)
    {
        var t = TimeSpan.FromMilliseconds(ms);
        return $@"{(int)t.TotalHours}:{t:mm}:{t:ss}";
    }

    public static string ToStr_s(this long s)
    {
        var t = TimeSpan.FromSeconds(s);
        return $@"{(int)t.TotalHours}:{t:mm}:{t:ss}";
    }

    public static string ToStr_ms(this int ms)
    {
        var t = TimeSpan.FromMilliseconds(ms);
        return $"{(int)t.TotalHours}:{t:mm}:{t:ss}";
    }
}