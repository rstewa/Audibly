using System.Collections.Generic;
using FlyleafLib.MediaFramework.MediaDemuxer;

namespace Audibly.Model;

public class Metadata
{
    public List<Demuxer.Chapter> Chapters { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public string Description { get; set; }
    public long Duration { get; set; }
}