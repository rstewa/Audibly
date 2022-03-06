using Chapter = FlyleafLib.MediaFramework.MediaDemuxer.Demuxer.Chapter;

namespace Audibly.Models
{
    public static class Extensions
    {
        public static bool InRange(this Chapter chapter, long curTime)
        {
            return curTime >= chapter.StartTime && curTime <= chapter.EndTime;
        }
    }
}
