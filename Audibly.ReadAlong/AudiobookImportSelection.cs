namespace Audibly.ReadAlong;

public sealed record AudiobookImportSelection(string AudioPath, string? EpubPath)
{
    public static AudiobookImportSelection Parse(IEnumerable<string> paths)
    {
        var files = paths.ToArray();
        var audio = files.Where(p => new[] { ".m4b", ".m4a", ".mp3" }.Contains(Path.GetExtension(p), StringComparer.OrdinalIgnoreCase)).ToArray();
        var epubs = files.Where(p => string.Equals(Path.GetExtension(p), ".epub", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (audio.Length != 1 || epubs.Length > 1 || files.Length != audio.Length + epubs.Length)
            throw new InvalidDataException("Select one audiobook and, optionally, its matching EPUB. Use the multiple audio files import for a book split across files.");
        return new(audio[0], epubs.SingleOrDefault());
    }
}
