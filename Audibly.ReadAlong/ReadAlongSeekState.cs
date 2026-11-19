namespace Audibly.ReadAlong;

public sealed record SourceOpenRequest(Guid Book, int Source, long Version);
public sealed record ReadAlongSeek(Guid Book, int Source, double Seconds);

// Selected source and loaded media are different states. Retain the latest
// click until that exact open request completes, including repeated clicks.
public sealed class ReadAlongSeekState
{
    private long _version;
    public SourceOpenRequest? Opening { get; private set; }
    public SourceOpenRequest? Loaded { get; private set; }
    public ReadAlongSeek? Pending { get; private set; }

    public SourceOpenRequest BeginOpen(Guid book, int source)
    {
        Opening = new(book, source, ++_version); Loaded = null;
        if (Pending is { } seek && (seek.Book != book || seek.Source != source)) Pending = null;
        return Opening;
    }
    public bool IsCurrent(SourceOpenRequest request) => request == Opening || request == Loaded;
    public bool IsLoaded(Guid book, int source) => Opening is null && Loaded is { } loaded && loaded.Book == book && loaded.Source == source;
    public bool Request(Guid book, int source, double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0 || source < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        if (IsLoaded(book, source)) { Pending = null; return true; }
        Pending = new(book, source, seconds); return false;
    }
    public ReadAlongSeek? Complete(SourceOpenRequest request)
    {
        if (Opening != request) return null;
        Loaded = request; Opening = null;
        if (Pending is not { } seek || seek.Book != request.Book || seek.Source != request.Source) return null;
        Pending = null; return seek;
    }
    public void Reset() { _version++; Opening = Loaded = null; Pending = null; }
}
