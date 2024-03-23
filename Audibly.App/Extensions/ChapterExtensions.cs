// Author: rstewa · https://github.com/rstewa
// Created: 3/21/2024
// Updated: 3/22/2024

using Audibly.Models;

namespace Audibly.App.Extensions;

public static class ChapterExtensions
{
    public static bool InRange(this ChapterInfo chapter, double curTime)
    {
        return curTime >= chapter.StartTime && curTime < chapter.EndTime;
    }
}