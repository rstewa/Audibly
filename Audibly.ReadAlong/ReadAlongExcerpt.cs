namespace Audibly.ReadAlong;

public sealed record ReadAlongExcerpt(Passage Passage, Cue ActiveCue, int Start, int End)
{
    // Bounded, exact EPUB substrings for a small floating caption window.
    public static ReadAlongExcerpt Create(Passage passage, Cue active, int maxCharacters = 160)
    {
        if (!passage.Cues.Contains(active) || active.Offset < 0 || active.Length <= 0 || active.Offset > passage.Text.Length - active.Length)
            throw new ArgumentException("The active cue must belong to the passage.");
        maxCharacters = Math.Clamp(maxCharacters, 32, 500);
        int start = 0, end;
        while (true)
        {
            end = Math.Min(passage.Text.Length, start + maxCharacters);
            if (end < passage.Text.Length)
                for (var i = end - 1; i > start; i--)
                    if (char.IsWhiteSpace(passage.Text[i])) { end = i + 1; break; }
            if (active.Offset < end)
            {
                end = Math.Max(end, active.Offset + active.Length);
                break;
            }
            start = end;
        }
        if (start > 0 && char.IsLowSurrogate(passage.Text[start])) start--;
        if (end < passage.Text.Length && char.IsHighSurrogate(passage.Text[end - 1])) end++;
        var excerpt = new Passage
        {
            Id = $"{passage.Id}:{start}:{end}:{active.SourceIndex}", Href = passage.Href, Fragment = passage.Fragment,
            Text = passage.Text[start..end],
            Cues = passage.Cues.Where(c => c.SourceIndex == active.SourceIndex && c.Offset >= start && c.Offset + c.Length <= end)
                .Select(c => new Cue { Id = c.Id, Fragment = c.Fragment, SourceIndex = c.SourceIndex,
                    StartSeconds = c.StartSeconds, EndSeconds = c.EndSeconds, Offset = c.Offset - start,
                    Length = c.Length, Granularity = c.Granularity, Quality = c.Quality }).ToList()
        };
        return new(excerpt, excerpt.Cues.First(c => c.Id == active.Id), start, end);
    }
}
