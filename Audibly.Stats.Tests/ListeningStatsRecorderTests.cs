using Audibly.Stats;

namespace Audibly.Stats.Tests;

public class ListeningStatsRecorderTests
{
    private static readonly Guid Book = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private DateTime _now = new(2026, 11, 14, 10, 0, 0);
    private int _library;
    private readonly List<Exception> _errors = [];

    private ListeningStatsRecorder Recorder(string path) =>
        new(new StatsStore(path), () => _now, () => _library, _errors.Add);

    private void Listen(ListeningStatsRecorder recorder, int seconds, double speed = 1.0)
    {
        recorder.OnInterrupted();
        for (var i = 0; i <= seconds; i++)
        {
            recorder.OnPosition(Book, 0, 1000 + i * speed, speed);
            _now = _now.AddSeconds(1);
        }
    }

    [Fact]
    public async Task FlushPersistsWhatWasObserved()
    {
        using var db = new TempDb();
        var recorder = Recorder(db.Path);
        Listen(recorder, 60, 2.0);
        await recorder.FlushAsync();
        var total = StatsSummary.Total(await new StatsStore(db.Path).LoadAsync());
        Assert.Equal(60, total.ListenedSeconds, 3);
        Assert.Equal(60, total.SavedSeconds, 3);
        Assert.Empty(_errors);
    }

    [Fact]
    public async Task FlushReturnsOnlyNewlyUnlockedTiers()
    {
        using var db = new TempDb();
        _library = 10;
        var recorder = Recorder(db.Path);
        var first = await recorder.FlushAsync();
        var collector = Assert.Single(first);
        Assert.Equal(("collector", 1), (collector.Definition.Id, collector.Tier));
        Listen(recorder, 5);
        Assert.Empty(await recorder.FlushAsync());
    }

    [Fact]
    public async Task RaisingATierIsReportedOnce()
    {
        using var db = new TempDb();
        _library = 10;
        var recorder = Recorder(db.Path);
        await recorder.FlushAsync();
        _library = 50;
        var raised = Assert.Single(await recorder.FlushAsync());
        Assert.Equal(("collector", 2), (raised.Definition.Id, raised.Tier));
        Assert.Empty(await recorder.FlushAsync());
        Assert.Empty(await Recorder(db.Path).FlushAsync());
    }

    [Fact]
    public async Task CompletionUnlocksFinisher()
    {
        using var db = new TempDb();
        var recorder = Recorder(db.Path);
        await recorder.FlushAsync();
        await recorder.OnCompletedAsync(Book);
        var unlocked = Assert.Single(await recorder.FlushAsync());
        Assert.Equal(("finisher", 1), (unlocked.Definition.Id, unlocked.Tier));
    }

    [Fact]
    public async Task StoredTierSurvivesShrinkingLibrary()
    {
        using var db = new TempDb();
        _library = 50;
        var recorder = Recorder(db.Path);
        await recorder.FlushAsync();
        _library = 5;
        var collector = (await recorder.LoadTrophiesAsync()).Single(s => s.Definition.Id == "collector");
        Assert.Equal(2, collector.Tier);
        Assert.Equal(5, collector.Value);
    }

    [Fact]
    public async Task LoadTrophiesReturnsTheWholeCatalog()
    {
        using var db = new TempDb();
        Assert.Equal(14, (await Recorder(db.Path).LoadTrophiesAsync()).Count);
    }

    [Fact]
    public async Task BrokenDatabaseNeverThrows()
    {
        using var db = new TempDb();
        await File.WriteAllTextAsync(db.Path, "not a database, just some text that is long enough to not be an empty file");
        var recorder = Recorder(db.Path);
        Listen(recorder, 5);
        Assert.Empty(await recorder.FlushAsync());
        await recorder.OnCompletedAsync(Book);
        Assert.Same(StatsData.Empty, await recorder.LoadAsync());
        Assert.All(await recorder.LoadTrophiesAsync(), state => Assert.Equal(0, state.Tier));
        Assert.NotEmpty(_errors);
    }

    [Fact]
    public async Task FailedWriteKeepsSlicesForTheNextFlush()
    {
        using var db = new TempDb();
        var recorder = Recorder(db.Path);
        await recorder.FlushAsync();
        Listen(recorder, 30);
        await using (new FileStream(db.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.Empty(await recorder.FlushAsync());
            Assert.NotEmpty(_errors);
        }

        await recorder.FlushAsync();
        Assert.Equal(30, StatsSummary.Total(await new StatsStore(db.Path).LoadAsync()).ListenedSeconds, 3);
    }

    [Fact]
    public async Task NothingNewMeansNoDatabaseAccess()
    {
        using var db = new TempDb();
        var recorder = Recorder(db.Path);
        Listen(recorder, 5);
        await recorder.FlushAsync();
        File.Delete(db.Path);
        Assert.Empty(await recorder.FlushAsync());
        Assert.False(File.Exists(db.Path));
    }

    [Fact]
    public async Task OverlappingFlushesDoNotCountTwice()
    {
        using var db = new TempDb();
        var recorder = Recorder(db.Path);
        Listen(recorder, 20);
        await Task.WhenAll(recorder.FlushAsync(), recorder.FlushAsync(), recorder.FlushAsync());
        Assert.Equal(20, StatsSummary.Total(await new StatsStore(db.Path).LoadAsync()).ListenedSeconds, 3);
    }
}
