using Audibly.Stats;
using Microsoft.Data.Sqlite;

namespace Audibly.Stats.Tests;

public sealed class TempDb : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"audibly-stats-{Guid.NewGuid():N}.db");
    public void Dispose()
    {
        try { File.Delete(Path); } catch (IOException) { }
    }
}

public class StatsStoreTests
{
    private static readonly Guid Book = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Other = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateOnly Day = new(2026, 11, 14);

    [Fact]
    public async Task AddingTheSameSliceTwiceSumsIt()
    {
        using var db = new TempDb();
        var store = new StatsStore(db.Path);
        await store.AddAsync([new ListeningSlice(Day, 10, Book, 60, 30)]);
        await store.AddAsync([new ListeningSlice(Day, 10, Book, 60, 30)]);
        var slice = Assert.Single((await store.LoadAsync()).Slices);
        Assert.Equal(new ListeningSlice(Day, 10, Book, 120, 60), slice);
    }

    [Fact]
    public async Task KeepsSlicesOfDifferentHoursAndBooksApart()
    {
        using var db = new TempDb();
        var store = new StatsStore(db.Path);
        await store.AddAsync([
            new ListeningSlice(Day, 10, Book, 60, 0), new ListeningSlice(Day, 11, Book, 60, 0),
            new ListeningSlice(Day, 10, Other, 60, 0), new ListeningSlice(Day.AddDays(1), 10, Book, 60, 0)
        ]);
        Assert.Equal(4, (await store.LoadAsync()).Slices.Count);
    }

    [Fact]
    public async Task DataSurvivesANewStoreInstance()
    {
        using var db = new TempDb();
        await new StatsStore(db.Path).AddAsync([new ListeningSlice(Day, 10, Book, 60, 30)]);
        Assert.Single((await new StatsStore(db.Path).LoadAsync()).Slices);
    }

    [Fact]
    public async Task EmptyDatabaseLoadsAsEmpty()
    {
        using var db = new TempDb();
        var data = await new StatsStore(db.Path).LoadAsync();
        Assert.Empty(data.Slices);
        Assert.Empty(data.Completions);
        Assert.Empty(await new StatsStore(db.Path).LoadTrophyTiersAsync());
    }

    [Fact]
    public async Task CompletionWithin24HoursIsNotRecordedTwice()
    {
        using var db = new TempDb();
        var store = new StatsStore(db.Path);
        var first = new DateTime(2026, 11, 14, 20, 0, 0);
        Assert.True(await store.RecordCompletionAsync(Book, first));
        Assert.False(await store.RecordCompletionAsync(Book, first.AddHours(23)));
        Assert.True(await store.RecordCompletionAsync(Other, first.AddHours(1)));
        Assert.True(await store.RecordCompletionAsync(Book, first.AddHours(25)));
        var completions = (await store.LoadAsync()).Completions;
        Assert.Equal(3, completions.Count);
        Assert.Contains(new Completion(Book, first), completions);
    }

    [Fact]
    public async Task TrophyTierNeverGoesDown()
    {
        using var db = new TempDb();
        var store = new StatsStore(db.Path);
        await store.SaveTrophyTierAsync("marathoner", 2, new DateTime(2026, 11, 14));
        await store.SaveTrophyTierAsync("marathoner", 1, new DateTime(2026, 11, 15));
        Assert.Equal(2, (await store.LoadTrophyTiersAsync())["marathoner"]);
    }

    [Fact]
    public async Task SeveralTrophyTiersAreSavedTogether()
    {
        using var db = new TempDb();
        var store = new StatsStore(db.Path);
        await store.SaveTrophyTierAsync("finisher", 3, new DateTime(2026, 11, 13));
        await store.SaveTrophyTiersAsync([("marathoner", 1), ("finisher", 2), ("collector", 2)], new DateTime(2026, 11, 14));
        var tiers = await store.LoadTrophyTiersAsync();
        Assert.Equal((1, 3, 2), (tiers["marathoner"], tiers["finisher"], tiers["collector"]));
    }

    [Fact]
    public async Task CorruptFileThrowsSqliteException()
    {
        using var db = new TempDb();
        await File.WriteAllTextAsync(db.Path, "not a database, just some text that is long enough to not be an empty file");
        await Assert.ThrowsAsync<SqliteException>(() => new StatsStore(db.Path).LoadAsync());
    }

    [Fact]
    public async Task FileIsNotLockedBetweenCalls()
    {
        using var db = new TempDb();
        await new StatsStore(db.Path).AddAsync([new ListeningSlice(Day, 10, Book, 60, 30)]);
        File.Move(db.Path, db.Path + ".moved");
        File.Move(db.Path + ".moved", db.Path);
    }
}
