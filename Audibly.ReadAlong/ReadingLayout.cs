using System.IO.Compression;
using System.Xml.Linq;

namespace Audibly.ReadAlong;

// Restore EPUB paragraphs for presentation without changing persisted word timing.
public static class ReadingLayout
{
    public static async Task<AlignmentMap> CreateAsync(AlignmentMap map, CancellationToken token = default)
    {
        if (map.IsReadingLayout) return map;
        using var zip = ZipFile.OpenRead(map.Epub.Path);
        var passages = new List<Passage>();
        foreach (var chapter in map.Passages.GroupBy(p => p.Href))
        {
            token.ThrowIfCancellationRequested();
            var href = Uri.UnescapeDataString(chapter.Key.Split('#')[0]).Replace('\\', '/').TrimStart('/');
            var entries = zip.Entries.Where(e => e.FullName == href || e.FullName.EndsWith('/' + href, StringComparison.Ordinal)).ToArray();
            if (entries.Length != 1 || entries[0].Length > 20 * 1024 * 1024)
            { passages.AddRange(chapter); continue; }
            List<XElement> blocks;
            try
            {
                await using var stream = entries[0].Open();
                var doc = await XDocument.LoadAsync(stream, LoadOptions.PreserveWhitespace, token);
                blocks = doc.Descendants().Where(IsBlock).Where(e => !e.Descendants().Any(IsBlock)).ToList();
            }
            catch (System.Xml.XmlException) { passages.AddRange(chapter); continue; }
            var blockIndex = 0; var offset = 0; XElement? lastBlock = null; Passage? display = null;
            var chapterStart = passages.Count;
            foreach (var sentence in chapter)
            {
                var foundBlock = -1; var foundOffset = -1;
                for (var i = blockIndex; i < blocks.Count; i++)
                {
                    var at = blocks[i].Value.IndexOf(sentence.Text, i == blockIndex ? offset : 0, StringComparison.Ordinal);
                    if (at < 0) continue;
                    foundBlock = i; foundOffset = at; break;
                }
                if (foundBlock < 0 || string.IsNullOrWhiteSpace(sentence.Text))
                {
                    // A structurally unusual chapter stays in its original layout;
                    // never duplicate a paragraph or guess where a sentence belongs.
                    passages.RemoveRange(chapterStart, passages.Count - chapterStart);
                    passages.AddRange(chapter); break;
                }
                blockIndex = foundBlock; offset = foundOffset + sentence.Text.Length;
                var block = blocks[blockIndex];
                if (lastBlock != block)
                {
                    display = new Passage { Id = sentence.Id + ":paragraph", Href = sentence.Href,
                        Fragment = sentence.Fragment, Text = block.Value };
                    passages.Add(display); lastBlock = block;
                }
                foreach (var cue in sentence.Cues)
                    display!.Cues.Add(Clone(cue, foundOffset + cue.Offset));
            }
        }
        var layout = new AlignmentMap { SchemaVersion = map.SchemaVersion, AudiobookId = map.AudiobookId,
            Engine = map.Engine, Status = map.Status, Language = map.Language, Epub = map.Epub,
            Sources = map.Sources, Passages = passages, IsReadingLayout = true };
        layout.Validate(map.AudiobookId, map.Sources.Count);
        return layout;
    }

    private static bool IsBlock(XElement e) => e.Name.LocalName is "p" or "li" or "h1" or "h2" or "h3" or "h4" or "h5" or "h6" or "blockquote" or "dt" or "dd";
    private static Cue Clone(Cue cue, int offset) => new() { Id = cue.Id, Fragment = cue.Fragment,
        SourceIndex = cue.SourceIndex, StartSeconds = cue.StartSeconds, EndSeconds = cue.EndSeconds,
        Offset = offset, Length = cue.Length, Granularity = cue.Granularity, Quality = cue.Quality };
}
