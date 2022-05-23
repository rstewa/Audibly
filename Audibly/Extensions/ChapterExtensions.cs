//   Author: Ryan Stewart
//   Date: 05/20/2022

using FlyleafLib.MediaFramework.MediaDemuxer;

namespace Audibly.Extensions;

public static class ChapterExtensions
{
    public static bool InRange(this Demuxer.Chapter chapter, double curTime)
    {
        return curTime >= chapter.StartTime && curTime < chapter.EndTime;
    }
}