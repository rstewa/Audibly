using FlyleafLib.MediaFramework.MediaDemuxer;

namespace Audibly.Extensions;

public static class ChapterExtensions
{
    public static bool InRange(this Demuxer.Chapter chapter, long curTime)
    {
        return curTime >= chapter.StartTime && curTime < chapter.EndTime;
    }
}