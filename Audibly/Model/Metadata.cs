//   Author: Ryan Stewart
//   Date: 05/20/2022

using FlyleafLib.MediaFramework.MediaDemuxer;
using System.Collections.Generic;

namespace Audibly.Model;

public class Metadata
{
    public List<Demuxer.Chapter> Chapters { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public string Description { get; set; }
    public long Duration { get; set; }
}