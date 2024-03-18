//   Author: Ryan Stewart
//   Date: 05/20/2022

using Audibly.Models;

namespace Audibly.App.Extensions;

public static class ChapterExtensions
{
    public static bool InRange(this ChapterInfo chapter, double curTime)
    {
        return curTime >= chapter.StartTime && curTime < chapter.EndTime;
    }
}