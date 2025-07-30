// Author: rstewa · https://github.com/rstewa
// Updated: 07/30/2025

namespace Audibly.App.Extensions;

public static class TimeExtensions
{
    public static double ToSeconds(this int ms)
    {
        return ms / 1000.0;
    }

    public static double ToSeconds(this double ms)
    {
        return ms / 1000.0;
    }
}