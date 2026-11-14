using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Audibly.Stats;

/// <summary>
///     Listening history in its own SQLite file, separate from the library database. Rows carry the
///     audiobook id without a foreign key, so history outlives a deleted audiobook. Every call opens
///     and closes its own connection; failures surface as <see cref="SqliteException" />.
/// </summary>
public sealed class StatsStore
{
    private const string DayFormat = "yyyy-MM-dd";
    private const string TimeFormat = "s";
    private static readonly TimeSpan CompletionCooldown = TimeSpan.FromHours(24);

    private const string Schema = """
        CREATE TABLE IF NOT EXISTS listening(
            day TEXT NOT NULL, hour INTEGER NOT NULL, book TEXT NOT NULL,
            listened REAL NOT NULL, saved REAL NOT NULL, PRIMARY KEY(day, hour, book));
        CREATE TABLE IF NOT EXISTS completions(book TEXT NOT NULL, completed_at TEXT NOT NULL);
        CREATE TABLE IF NOT EXISTS trophies(id TEXT PRIMARY KEY, tier INTEGER NOT NULL, unlocked_at TEXT NOT NULL);
        """;

    private readonly string _connectionString;

    public StatsStore(string databasePath)
    {
        _connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath, Pooling = false }.ToString();
    }

    public Task AddAsync(IReadOnlyList<ListeningSlice> slices) => Task.Run(() =>
    {
        if (slices.Count == 0) return;
        using var db = Open();
        using var transaction = db.BeginTransaction();
        using var command = db.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO listening(day, hour, book, listened, saved) VALUES ($day, $hour, $book, $listened, $saved)
            ON CONFLICT(day, hour, book) DO UPDATE SET
                listened = listened + excluded.listened, saved = saved + excluded.saved
            """;
        var day = command.Parameters.Add("$day", SqliteType.Text);
        var hour = command.Parameters.Add("$hour", SqliteType.Integer);
        var book = command.Parameters.Add("$book", SqliteType.Text);
        var listened = command.Parameters.Add("$listened", SqliteType.Real);
        var saved = command.Parameters.Add("$saved", SqliteType.Real);
        foreach (var slice in slices)
        {
            day.Value = slice.Day.ToString(DayFormat, CultureInfo.InvariantCulture);
            hour.Value = slice.Hour;
            book.Value = slice.AudiobookId.ToString("D");
            listened.Value = slice.ListenedSeconds;
            saved.Value = slice.SavedSeconds;
            command.ExecuteNonQuery();
        }

        transaction.Commit();
    });

    /// <summary>Records a finished audiobook; a repeat for the same book within 24 hours is ignored.</summary>
    /// <returns>False when the completion was a repeat and nothing was written.</returns>
    public Task<bool> RecordCompletionAsync(Guid audiobookId, DateTime localCompletedAt) => Task.Run(() =>
    {
        using var db = Open();
        using var transaction = db.BeginTransaction();
        using (var latest = db.CreateCommand())
        {
            latest.Transaction = transaction;
            latest.CommandText = "SELECT MAX(completed_at) FROM completions WHERE book = $book";
            latest.Parameters.AddWithValue("$book", audiobookId.ToString("D"));
            if (latest.ExecuteScalar() is string text &&
                localCompletedAt - ParseTime(text) < CompletionCooldown) return false;
        }

        using (var insert = db.CreateCommand())
        {
            insert.Transaction = transaction;
            insert.CommandText = "INSERT INTO completions(book, completed_at) VALUES ($book, $at)";
            insert.Parameters.AddWithValue("$book", audiobookId.ToString("D"));
            insert.Parameters.AddWithValue("$at", localCompletedAt.ToString(TimeFormat, CultureInfo.InvariantCulture));
            insert.ExecuteNonQuery();
        }

        transaction.Commit();
        return true;
    });

    public Task<StatsData> LoadAsync() => Task.Run(() =>
    {
        using var db = Open();
        var slices = new List<ListeningSlice>();
        using (var command = db.CreateCommand())
        {
            command.CommandText = "SELECT day, hour, book, listened, saved FROM listening ORDER BY day, hour";
            using var reader = command.ExecuteReader();
            while (reader.Read())
                slices.Add(new ListeningSlice(
                    DateOnly.ParseExact(reader.GetString(0), DayFormat, CultureInfo.InvariantCulture),
                    reader.GetInt32(1), Guid.Parse(reader.GetString(2)), reader.GetDouble(3), reader.GetDouble(4)));
        }

        var completions = new List<Completion>();
        using (var command = db.CreateCommand())
        {
            command.CommandText = "SELECT book, completed_at FROM completions ORDER BY completed_at";
            using var reader = command.ExecuteReader();
            while (reader.Read())
                completions.Add(new Completion(Guid.Parse(reader.GetString(0)), ParseTime(reader.GetString(1))));
        }

        return new StatsData(slices, completions);
    });

    public Task<IReadOnlyDictionary<string, int>> LoadTrophyTiersAsync() => Task.Run(() =>
    {
        using var db = Open();
        using var command = db.CreateCommand();
        command.CommandText = "SELECT id, tier FROM trophies";
        using var reader = command.ExecuteReader();
        var tiers = new Dictionary<string, int>();
        while (reader.Read()) tiers[reader.GetString(0)] = reader.GetInt32(1);
        return (IReadOnlyDictionary<string, int>)tiers;
    });

    /// <summary>Remembers the highest tier reached; a lower tier never replaces a higher one.</summary>
    public Task SaveTrophyTierAsync(string trophyId, int tier, DateTime localUnlockedAt) =>
        SaveTrophyTiersAsync([(trophyId, tier)], localUnlockedAt);

    /// <summary>Saves several tiers together: either all of them are stored or none.</summary>
    public Task SaveTrophyTiersAsync(IReadOnlyList<(string TrophyId, int Tier)> tiers, DateTime localUnlockedAt) =>
        Task.Run(() =>
        {
            if (tiers.Count == 0) return;
            using var db = Open();
            using var transaction = db.BeginTransaction();
            using var command = db.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO trophies(id, tier, unlocked_at) VALUES ($id, $tier, $at)
                ON CONFLICT(id) DO UPDATE SET tier = excluded.tier, unlocked_at = excluded.unlocked_at
                WHERE excluded.tier > trophies.tier
                """;
            var id = command.Parameters.Add("$id", SqliteType.Text);
            var tier = command.Parameters.Add("$tier", SqliteType.Integer);
            command.Parameters.AddWithValue("$at", localUnlockedAt.ToString(TimeFormat, CultureInfo.InvariantCulture));
            foreach (var (trophyId, value) in tiers)
            {
                id.Value = trophyId;
                tier.Value = value;
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        });

    private SqliteConnection Open()
    {
        var db = new SqliteConnection(_connectionString);
        try
        {
            db.Open();
            using var command = db.CreateCommand();
            command.CommandText = Schema;
            command.ExecuteNonQuery();
            return db;
        }
        catch
        {
            db.Dispose();
            throw;
        }
    }

    private static DateTime ParseTime(string text) =>
        DateTime.ParseExact(text, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None);
}
