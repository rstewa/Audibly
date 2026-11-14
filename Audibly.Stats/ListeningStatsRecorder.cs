namespace Audibly.Stats;

/// <summary>
///     Collects listening time while a book plays and writes it out in batches. Nothing here throws:
///     statistics must never get in the way of playback, so failures go to the error callback and the
///     collected time is kept for the next flush.
/// </summary>
public sealed class ListeningStatsRecorder
{
    private readonly StatsStore _store;
    private readonly Func<DateTime> _localNow;
    private readonly Func<int> _libraryCount;
    private readonly Action<Exception>? _onError;
    private readonly ListeningTracker _tracker = new();
    private readonly object _trackerLock = new();
    private readonly SemaphoreSlim _flushing = new(1, 1);
    private List<ListeningSlice> _unsaved = [];
    private bool _completionPending;
    private int? _evaluatedLibraryCount;

    public ListeningStatsRecorder(StatsStore store, Func<DateTime> localNow, Func<int> libraryCount,
        Action<Exception>? onError = null)
    {
        _store = store;
        _localNow = localNow;
        _libraryCount = libraryCount;
        _onError = onError;
    }

    /// <summary>Report the playback position while the book is playing.</summary>
    public void OnPosition(Guid audiobookId, int sourceIndex, double positionSeconds, double speed)
    {
        try
        {
            lock (_trackerLock) _tracker.Observe(audiobookId, sourceIndex, positionSeconds, speed, _localNow());
        }
        catch (Exception e)
        {
            Report(e);
        }
    }

    /// <summary>Report that playback stopped being continuous: pause, seek or another book.</summary>
    public void OnInterrupted()
    {
        lock (_trackerLock) _tracker.Break();
    }

    /// <summary>Report that the listener reached the end of an audiobook.</summary>
    public async Task OnCompletedAsync(Guid audiobookId)
    {
        try
        {
            if (await _store.RecordCompletionAsync(audiobookId, _localNow()).ConfigureAwait(false))
                _completionPending = true;
        }
        catch (Exception e)
        {
            Report(e);
        }
    }

    /// <summary>Writes the collected time and returns the trophies whose tier went up, at their new tier.</summary>
    public async Task<IReadOnlyList<TrophyState>> FlushAsync()
    {
        await _flushing.WaitAsync().ConfigureAwait(false);
        try
        {
            IReadOnlyList<ListeningSlice> drained;
            lock (_trackerLock) drained = _tracker.Drain();
            _unsaved.AddRange(drained);

            var library = _libraryCount();
            if (_unsaved.Count == 0 && !_completionPending && _evaluatedLibraryCount == library) return [];

            await _store.AddAsync(_unsaved).ConfigureAwait(false);
            _unsaved = [];

            var states = TrophyCatalog.Evaluate(new TrophyInput(await _store.LoadAsync().ConfigureAwait(false), library));
            var stored = await _store.LoadTrophyTiersAsync().ConfigureAwait(false);
            var raised = states.Where(state => state.Tier > stored.GetValueOrDefault(state.Definition.Id)).ToList();
            foreach (var state in raised)
                await _store.SaveTrophyTierAsync(state.Definition.Id, state.Tier, _localNow()).ConfigureAwait(false);

            _completionPending = false;
            _evaluatedLibraryCount = library;
            return raised;
        }
        catch (Exception e)
        {
            Report(e);
            return [];
        }
        finally
        {
            _flushing.Release();
        }
    }

    public async Task<StatsData> LoadAsync()
    {
        try
        {
            return await _store.LoadAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Report(e);
            return StatsData.Empty;
        }
    }

    /// <summary>Every trophy with its current value; a tier that was reached once stays reached.</summary>
    public async Task<IReadOnlyList<TrophyState>> LoadTrophiesAsync()
    {
        var library = 0;
        try
        {
            library = _libraryCount();
            var states = TrophyCatalog.Evaluate(new TrophyInput(await _store.LoadAsync().ConfigureAwait(false), library));
            var stored = await _store.LoadTrophyTiersAsync().ConfigureAwait(false);
            return states
                .Select(state => state with { Tier = Math.Max(state.Tier, stored.GetValueOrDefault(state.Definition.Id)) })
                .ToList();
        }
        catch (Exception e)
        {
            Report(e);
            return TrophyCatalog.Evaluate(new TrophyInput(StatsData.Empty, library))
                .Select(state => state with { Tier = 0 }).ToList();
        }
    }

    private void Report(Exception e)
    {
        try
        {
            _onError?.Invoke(e);
        }
        catch
        {
            // the error callback is the last resort; nothing is left to tell
        }
    }
}
