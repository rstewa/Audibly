using System.Text.Json;

namespace Audibly.ReadAlong;

public sealed class BookAssociation
{
    public string EpubPath { get; set; } = "";
    public string? MapPath { get; set; }
    public string Status { get; set; } = "attached";
    public string? Error { get; set; }
}

// Sidecars live beside Audibly.db and use the existing database audiobook GUID.
// No timing payloads or processing state are added to the library's relational rows.
public sealed class ReadAlongStore(string root)
{
    public string BookFolder(Guid id) => Path.Combine(root, "ReadAlong", id.ToString("D"));
    private string AssociationPath(Guid id) => Path.Combine(BookFolder(id), "book.json");

    public async Task<BookAssociation?> LoadAsync(Guid id, CancellationToken token = default)
    {
        if (!File.Exists(AssociationPath(id))) return null;
        await using var file = File.OpenRead(AssociationPath(id));
        return await JsonSerializer.DeserializeAsync<BookAssociation>(file, AlignmentMap.Json, token);
    }

    public async Task SaveAsync(Guid id, BookAssociation association, CancellationToken token = default)
    {
        Directory.CreateDirectory(BookFolder(id));
        var temporary = AssociationPath(id) + "." + Guid.NewGuid() + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(association, AlignmentMap.Json), token);
            File.Move(temporary, AssociationPath(id), true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public async Task<BookAssociation> AttachAsync(Guid id, string epub, CancellationToken token = default)
    {
        // Preserve the user's file and keep a managed copy even if the original is moved.
        Directory.CreateDirectory(BookFolder(id));
        var path = Path.Combine(BookFolder(id), Guid.NewGuid() + ".epub");
        await using (var source = File.OpenRead(epub))
        await using (var destination = File.Create(path))
            await source.CopyToAsync(destination, token);
        var association = new BookAssociation { EpubPath = path };
        await SaveAsync(id, association, token);
        return association;
    }

    public async Task<AlignmentMap?> LoadMapAsync(Guid id, BookAssociation association,
        IReadOnlyList<string> sources, CancellationToken token = default)
    {
        if (association.MapPath is null || !File.Exists(association.MapPath)) return null;
        await using var file = File.OpenRead(association.MapPath);
        var map = await JsonSerializer.DeserializeAsync<AlignmentMap>(file, AlignmentMap.Json, token)
            ?? throw new InvalidDataException("Empty alignment map.");
        map.Validate(id, sources.Count);
        if (!await map.Epub.MatchesAsync(association.EpubPath, token)) throw new InvalidDataException("EPUB changed; generate alignment again.");
        for (var i = 0; i < sources.Count; i++)
            if (!await map.Sources[i].MatchesAsync(sources[i], token)) throw new InvalidDataException("Audio changed; generate alignment again.");
        return map;
    }
}
