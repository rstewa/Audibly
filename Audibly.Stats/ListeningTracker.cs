namespace Audibly.Stats;

/// <summary>
///     Turns the player's position ticks into listening time. Only steady forward movement counts:
///     seeks, chapter jumps and file changes move the position too far (or backwards) and are ignored.
///     Not thread-safe; the player reports positions from one thread.
/// </summary>
public sealed class ListeningTracker
{
    /// <summary>The longest real time one tick may cover before it is treated as a jump.</summary>
    public const double MaxTickRealSeconds = 3.0;

    private readonly Dictionary<(DateOnly Day, int Hour, Guid Book), (double Listened, double Saved)> _slices = [];
    private (Guid Book, int Source, double Position)? _last;

    /// <param name="positionSeconds">Position inside the current source file, in audiobook time.</param>
    /// <param name="speed">Playback speed; audiobook time passes this much faster than real time.</param>
    public void Observe(Guid audiobookId, int sourceIndex, double positionSeconds, double speed, DateTime localNow)
    {
        var last = _last;
        _last = double.IsFinite(positionSeconds) ? (audiobookId, sourceIndex, positionSeconds) : null;
        if (last is not { } previous || previous.Book != audiobookId || previous.Source != sourceIndex) return;
        if (!double.IsFinite(speed) || speed <= 0 || !double.IsFinite(positionSeconds)) return;

        var content = positionSeconds - previous.Position;
        var listened = content / speed;
        if (!(listened > 0) || listened > MaxTickRealSeconds) return;

        var key = (DateOnly.FromDateTime(localNow), localNow.Hour, audiobookId);
        _slices.TryGetValue(key, out var sum);
        _slices[key] = (sum.Listened + listened, sum.Saved + Math.Max(0, content - listened));
    }

    /// <summary>Call when playback stops being continuous (pause, seek, another book).</summary>
    public void Break() => _last = null;

    /// <summary>Returns what was counted since the last call and starts over.</summary>
    public IReadOnlyList<ListeningSlice> Drain()
    {
        var result = _slices
            .Select(s => new ListeningSlice(s.Key.Day, s.Key.Hour, s.Key.Book, s.Value.Listened, s.Value.Saved))
            .ToList();
        _slices.Clear();
        return result;
    }
}
