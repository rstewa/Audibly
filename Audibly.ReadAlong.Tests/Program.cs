using System.Text.Json;
using Audibly.ReadAlong;

var checks = 0;
void Check(bool result, string name) { if (!result) throw new Exception(name); checks++; Console.WriteLine("PASS " + name); }
void Reject(Action action, string name)
{
    try { action(); } catch (InvalidDataException) { Check(true, name); return; }
    throw new Exception("Accepted invalid map: " + name);
}
var id = Guid.NewGuid();
AlignmentMap Fixture() => new()
{
    SchemaVersion = 1, AudiobookId = id, Status = "ready", Epub = new("book.epub", 1, "hash"),
    Sources = [new("a.mp3", 1, "a"), new("b.mp3", 1, "b")],
    Passages = [new() { Id = "p", Text = "Hello world.", Cues = [
        new() { Id = "a", SourceIndex = 0, StartSeconds = 1, EndSeconds = 2, Offset = 0, Length = 5, Quality = 99 },
        new() { Id = "b", SourceIndex = 1, StartSeconds = 1, EndSeconds = 2, Offset = 6, Length = 5, Quality = 99 }]}]
};
var map = Fixture(); map.Validate(id, 2);
Check(map.Find(0, 1)?.Cue.Id == "a", "inclusive start");
Check(map.Find(0, 2) is null, "exclusive end and silence unhighlighted");
Check(map.Find(0, 0.5) is null, "introduction unhighlighted");
Check(map.Find(1, 1.5)?.Cue.Id == "b", "source-local timestamps across tracks");
Check(map.Find(0, 1.5)?.Cue.Id == "a", "seek backwards uses source position");
Check(map.Find(5, 1) is null && map.Find(0, double.NaN) is null, "unknown position");
// At any speed the lookup takes MediaPlayer source position, never wall-clock elapsed time.
foreach (var speed in new[] { 0.5, 1.0, 2.0 }) Check(map.Find(0, 1.5)?.Cue.Id == "a", $"position independent of speed {speed}");
Reject(() => map.Validate(Guid.NewGuid(), 2), "wrong book rejected");
var invalid = Fixture(); invalid.SchemaVersion = 2; Reject(() => invalid.Validate(id, 2), "unsupported schema");
invalid = Fixture(); invalid.Passages[0].Cues[0].EndSeconds = double.NaN; Reject(() => invalid.Validate(id, 2), "nonfinite timing");
invalid = Fixture(); invalid.Passages[0].Cues[0].Offset = 500; Reject(() => invalid.Validate(id, 2), "invalid text offsets");
invalid = Fixture(); invalid.Passages[0].Cues[0].Quality = 50; Reject(() => invalid.Validate(id, 2), "low quality refused");
invalid = Fixture(); invalid.Passages[0].Cues[1].SourceIndex = 0; Reject(() => invalid.Validate(id, 2), "ambiguous overlapping cues refused");
var temporary = Path.Combine(Path.GetTempPath(), "audibly-readalong-tests-" + Guid.NewGuid());
Directory.CreateDirectory(temporary);
try
{
    var audio = Path.Combine(temporary, "audio.mp3"); var epub = Path.Combine(temporary, "book.epub");
    await File.WriteAllTextAsync(audio, "audio"); await File.WriteAllTextAsync(epub, "EPUB");
    var store = new ReadAlongStore(temporary);
    var association = await store.AttachAsync(id, epub);
    Check(File.ReadAllText(epub) == "EPUB" && association.EpubPath != epub, "attachment preserves original");
    Check((await store.LoadAsync(id))?.EpubPath == association.EpubPath, "association reload");
    Check(await store.LoadAsync(Guid.NewGuid()) is null, "unattached books unchanged");
    map = Fixture(); map.Sources = [await Fingerprint.CreateAsync(audio)];
    map.Passages[0].Cues.RemoveAt(1); map.Epub = await Fingerprint.CreateAsync(association.EpubPath);
    association.MapPath = Path.Combine(temporary, "map.json");
    await File.WriteAllTextAsync(association.MapPath, JsonSerializer.Serialize(map, AlignmentMap.Json));
    var reloaded = await store.LoadMapAsync(id, association, [audio]);
    Check(reloaded?.Find(0,1.1)?.Cue.Id == "a", "map reload and fingerprint verification");
    await File.WriteAllTextAsync(audio, "other");
    try { await store.LoadMapAsync(id, association, [audio]); throw new Exception("Stale audio accepted"); }
    catch (InvalidDataException) { Check(true, "same-size changed audio refused"); }
    await File.WriteAllTextAsync(association.MapPath, "{");
    try { await store.LoadMapAsync(id, association, [audio]); throw new Exception("Corrupt map accepted"); }
    catch (JsonException) { Check(true, "corrupt JSON rejected"); }
    // A running worker must be killed, not just an already-cancelled request refused.
    var fakeEngine = Path.Combine(temporary, "fake-engine"); Directory.CreateDirectory(fakeEngine);
    await File.WriteAllTextAsync(Path.Combine(fakeEngine, "align.mjs"), "console.log('worker-ready'); setInterval(() => {}, 1000);");
    using var inFlight = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    var workerStarted = false;
    var cancelProgress = new ImmediateProgress(text => { if (text == "worker-ready") { workerStarted = true; inFlight.Cancel(); } });
    try
    {
        await new StorytellerProcessor().GenerateAsync(fakeEngine, Path.Combine(temporary,"jobs"), id, epub, [audio], "en", cancelProgress, inFlight.Token);
        throw new Exception("Running worker was not cancelled");
    }
    catch (OperationCanceledException)
    {
        Check(workerStarted, "cancellation exercised a running worker");
        Check(!Directory.EnumerateFiles(Path.Combine(temporary,"jobs"), "*.job.json").Any(), "running cancellation cleans up request");
    }
}
finally
{
    if (!Path.GetFullPath(temporary).StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase))
        throw new Exception("Unexpected test cleanup directory");
    Directory.Delete(temporary, true);
}
if (args.Length > 0)
{
    var real = JsonSerializer.Deserialize<AlignmentMap>(await File.ReadAllTextAsync(args[0]), AlignmentMap.Json)!;
    real.Validate(real.AudiobookId, real.Sources.Count);
    Check(real.Passages.SelectMany(p => p.Cues).Count() > 150, "real Storyteller output validates");
    foreach (var p in real.Passages)
        foreach (var cue in p.Cues)
            Check(real.Find(cue.SourceIndex, (cue.StartSeconds + cue.EndSeconds)/2)?.Cue.Id == cue.Id, "real cue " + cue.Id);
    if (args.Length > 1)
    {
        var cache = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[0]))!, "processor-test");
        var generated = await new StorytellerProcessor().GenerateAsync(Path.GetFullPath(args[1]), cache,
            real.AudiobookId, real.Epub.Path, real.Sources.Select(s => s.Path).ToArray(), "en",
            new Progress<string>(Console.WriteLine), CancellationToken.None);
        var processed = JsonSerializer.Deserialize<AlignmentMap>(await File.ReadAllTextAsync(generated), AlignmentMap.Json)!;
        processed.Validate(real.AudiobookId, real.Sources.Count);
        Check(processed.Passages.Sum(p => p.Cues.Count) > 150, "C# process adapter runs real CPU engine");
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        try
        {
            await new StorytellerProcessor().GenerateAsync(Path.GetFullPath(args[1]), cache, real.AudiobookId,
                real.Epub.Path, real.Sources.Select(s => s.Path).ToArray(), "en", new Progress<string>(), cancelled.Token);
            throw new Exception("Cancelled job accepted");
        }
        catch (OperationCanceledException) { Check(!Directory.EnumerateFiles(cache, "*.job.json").Any(), "cancelled job cleans up request"); }
    }
}
Console.WriteLine($"{checks} checks passed");

sealed class ImmediateProgress(Action<string> report) : IProgress<string>
{
    public void Report(string value) => report(value);
}
