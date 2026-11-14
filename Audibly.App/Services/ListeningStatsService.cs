using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Audibly.App.ViewModels;
using Audibly.Stats;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.Services;

/// <summary>
///     Feeds the player's position into the listening statistics, writes them out every 30 seconds
///     and announces trophies as they unlock. No member throws; statistics never interrupt playback.
/// </summary>
public sealed class ListeningStatsService
{
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ShutdownFlushLimit = TimeSpan.FromSeconds(2);

    private readonly ListeningStatsRecorder _recorder;
    private DispatcherQueue? _dispatcherQueue;
    private DispatcherQueueTimer? _timer;
    private bool _changed;

    public ListeningStatsService(string databasePath)
    {
        _recorder = new ListeningStatsRecorder(new StatsStore(databasePath), () => DateTime.Now,
            LibraryCount, e => App.ViewModel.LoggingService.LogError(e));
    }

    /// <summary>
    ///     Titles in the whole library. <c>Audiobooks</c> is only what the library page currently shows
    ///     and shrinks while a search or filter is active.
    /// </summary>
    public static int LibraryCount() => App.ViewModel.AudiobooksForFilter.Count;

    /// <summary>Raised on the UI thread after new listening time or a completion was saved.</summary>
    public event Action? Updated;

    /// <summary>Call from the UI thread with the position of the book that is playing.</summary>
    public void OnPosition(Guid audiobookId, int sourceIndex, double positionSeconds, double speed)
    {
        EnsureTimer();
        _recorder.OnPosition(audiobookId, sourceIndex, positionSeconds, speed);
        _changed = true;
    }

    /// <summary>Call when playback pauses, seeks or switches to another book.</summary>
    public void OnInterrupted()
    {
        _recorder.OnInterrupted();
        if (_changed) _ = FlushAsync();
    }

    /// <summary>Call when the listener reaches the end of an audiobook.</summary>
    public async void OnCompleted(Guid audiobookId)
    {
        await _recorder.OnCompletedAsync(audiobookId);
        _changed = true;
        await FlushAsync();
    }

    public async Task FlushAsync()
    {
        var changed = _changed;
        _changed = false;
        var unlocked = await _recorder.FlushAsync();
        if (!changed && unlocked.Count == 0) return;

        void Announce()
        {
            try
            {
                foreach (var state in unlocked)
                    App.ViewModel.EnqueueNotification(new Notification
                    {
                        Message = $"Trophy unlocked: {state.Definition.Name} ({TrophyCatalog.TierName(state.Tier)})",
                        Severity = InfoBarSeverity.Success
                    });
                Updated?.Invoke();
            }
            catch (Exception e)
            {
                App.ViewModel.LoggingService.LogError(e);
            }
        }

        if (_dispatcherQueue is { HasThreadAccess: false } queue) queue.TryEnqueue(Announce);
        else Announce();
    }

    /// <summary>Saves what is still in memory before the app closes; gives up after two seconds.</summary>
    public void FlushBeforeExit()
    {
        try
        {
            _timer?.Stop();
            _recorder.FlushAsync().Wait(ShutdownFlushLimit);
        }
        catch (Exception e)
        {
            App.ViewModel.LoggingService.LogError(e);
        }
    }

    public Task<StatsData> LoadAsync() => _recorder.LoadAsync();

    /// <summary>
    ///     Flushes first, so a tier reached without playback (a grown library) is stored and announced
    ///     before it is shown, and cannot be lost again when the library shrinks.
    /// </summary>
    public async Task<IReadOnlyList<TrophyState>> LoadTrophiesAsync()
    {
        await FlushAsync();
        return await _recorder.LoadTrophiesAsync();
    }

    private void EnsureTimer()
    {
        if (_timer != null) return;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        if (_dispatcherQueue == null) return;
        _timer = _dispatcherQueue.CreateTimer();
        _timer.Interval = FlushInterval;
        _timer.Tick += (_, _) => _ = FlushAsync();
        _timer.Start();
    }
}
