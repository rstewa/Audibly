namespace Audibly.Stats;

/// <summary>Listening within one hour of one local day for one audiobook, in real seconds.</summary>
public sealed record ListeningSlice(DateOnly Day, int Hour, Guid AudiobookId, double ListenedSeconds, double SavedSeconds);

/// <summary>An audiobook that was listened to the end, in local time.</summary>
public sealed record Completion(Guid AudiobookId, DateTime CompletedAt);

public sealed record StatsData(IReadOnlyList<ListeningSlice> Slices, IReadOnlyList<Completion> Completions)
{
    public static readonly StatsData Empty = new([], []);
}
