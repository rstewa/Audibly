// Author: rstewa · https://github.com/rstewa
// Created: 12/02/2024
// Updated: 12/02/2024

namespace Audibly.App.Extensions;

public static class TimeExtensions
{
    public static double ToSeconds(this int ms)
    {
        return ms / 1000.0;
    }
}