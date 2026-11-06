using System.Security.Cryptography;
using System.Text.Json;

namespace Audibly.ReadAlong;

public sealed record Fingerprint(string Path, long Size, string Sha256)
{
    public static async Task<Fingerprint> CreateAsync(string path, CancellationToken token = default)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, true);
        var size = stream.Length;
        var hash = await SHA256.HashDataAsync(stream, token);
        return new(System.IO.Path.GetFullPath(path), size, Convert.ToHexString(hash).ToLowerInvariant());
    }

    public async Task<bool> MatchesAsync(string path, CancellationToken token = default) =>
        File.Exists(path) && new FileInfo(path).Length == Size &&
        string.Equals(Sha256, (await CreateAsync(path, token)).Sha256, StringComparison.OrdinalIgnoreCase);
}

public sealed class Cue
{
    public string Id { get; set; } = "";
    public string Fragment { get; set; } = "";
    public int SourceIndex { get; set; }
    public double StartSeconds { get; set; }
    public double EndSeconds { get; set; }
    public int Offset { get; set; }
    public int Length { get; set; }
    public string Granularity { get; set; } = "word";
    public double Quality { get; set; }
}

public sealed class Passage
{
    public string Id { get; set; } = "";
    public string Href { get; set; } = "";
    public string Fragment { get; set; } = "";
    public string Text { get; set; } = "";
    public List<Cue> Cues { get; set; } = [];
    public override string ToString() => Text;
}

public sealed class AlignmentMap
{
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsReadingLayout { get; set; }
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public int SchemaVersion { get; set; }
    public Guid AudiobookId { get; set; }
    public string Engine { get; set; } = "";
    public string Status { get; set; } = "";
    public string Language { get; set; } = "en";
    public Fingerprint Epub { get; set; } = new("", 0, "");
    public List<Fingerprint> Sources { get; set; } = [];
    public List<Passage> Passages { get; set; } = [];
    private Dictionary<int, (Cue Cue, Passage Passage)[]> _tracks = [];

    public void Validate(Guid audiobookId, int sourceCount)
    {
        if (SchemaVersion != 1 || AudiobookId != audiobookId || Status != "ready" || Sources.Count != sourceCount ||
            Sources.Count == 0 || Passages.Count == 0 || Epub is null || string.IsNullOrEmpty(Epub.Sha256))
            throw new InvalidDataException("Alignment does not belong to this book or has an unsupported format.");
        var entries = new List<(Cue Cue, Passage Passage)>();
        var ids = new HashSet<string>();
        foreach (var passage in Passages)
        {
            if (passage.Text is null || passage.Cues is null) throw new InvalidDataException("Invalid EPUB passage.");
            foreach (var cue in passage.Cues)
            {
                if (!ids.Add(cue.Id) || cue.SourceIndex < 0 || cue.SourceIndex >= sourceCount ||
                    !double.IsFinite(cue.StartSeconds) || !double.IsFinite(cue.EndSeconds) ||
                    cue.StartSeconds < 0 || cue.EndSeconds <= cue.StartSeconds ||
                    cue.Offset < 0 || cue.Length <= 0 || cue.Offset > passage.Text.Length - cue.Length ||
                    cue.Granularity is not ("word" or "sentence") || !double.IsFinite(cue.Quality) || cue.Quality < 80)
                    throw new InvalidDataException("Invalid or unreliable alignment cue.");
                entries.Add((cue, passage));
            }
        }
        if (entries.Count == 0) throw new InvalidDataException("No reliable matching passages.");
        _tracks = entries.GroupBy(e => e.Cue.SourceIndex).ToDictionary(g => g.Key,
            g => g.OrderBy(e => e.Cue.StartSeconds).ToArray());
        foreach (var track in _tracks.Values)
            for (var i = 1; i < track.Length; i++)
                if (track[i].Cue.StartSeconds < track[i - 1].Cue.EndSeconds)
                    throw new InvalidDataException("Overlapping timing cues are ambiguous.");
    }

    // Media position is source-local seconds, independent of playback speed and chapter offsets.
    public (Cue Cue, Passage Passage)? Find(int sourceIndex, double seconds)
    {
        if (!double.IsFinite(seconds) || !_tracks.TryGetValue(sourceIndex, out var track)) return null;
        int low = 0, high = track.Length - 1;
        while (low <= high)
        {
            var mid = low + (high - low) / 2;
            var entry = track[mid];
            if (seconds < entry.Cue.StartSeconds) high = mid - 1;
            else if (seconds >= entry.Cue.EndSeconds) low = mid + 1;
            else return entry;
        }
        return null;
    }

    // Nearby text remains readable through pauses; callers must not highlight
    // this contextual cue unless Find confirms it is actually being spoken.
    public (Cue Cue, Passage Passage)? FindContext(int sourceIndex, double seconds, double maxDistance = 12)
    {
        if (!double.IsFinite(seconds) || seconds < 0 || !_tracks.TryGetValue(sourceIndex, out var track)) return null;
        int low = 0, high = track.Length;
        while (low < high)
        {
            var mid = low + (high - low) / 2;
            if (track[mid].Cue.StartSeconds <= seconds) low = mid + 1; else high = mid;
        }
        if (low > 0 && seconds - track[low - 1].Cue.EndSeconds <= maxDistance) return track[low - 1];
        if (low < track.Length && track[low].Cue.StartSeconds - seconds <= maxDistance) return track[low];
        return null;
    }
}
