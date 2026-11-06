using System.Text.Json;
using Audibly.ReadAlong;

if (args.Length is 2 or 3 && args[0] == "--validate-map")
{
    var actual = JsonSerializer.Deserialize<AlignmentMap>(await File.ReadAllTextAsync(args[1]), AlignmentMap.Json)!;
    actual.Validate(actual.AudiobookId, actual.Sources.Count);
    var association = new BookAssociation { EpubPath = actual.Epub.Path, MapPath = Path.GetFullPath(args[1]) };
    var loaded = await new ReadAlongStore(Path.GetDirectoryName(Path.GetFullPath(args[1]))!).LoadMapAsync(actual.AudiobookId, association, actual.Sources.Select(s=>s.Path).ToArray());
    var layout = await ReadingLayout.CreateAsync(loaded!);
    var originalCues = loaded!.Passages.SelectMany(p => p.Cues).ToDictionary(c => c.Id);
    foreach (var paragraph in layout.Passages)
        foreach (var cue in paragraph.Cues)
        {
            var original = loaded.Find(cue.SourceIndex, (cue.StartSeconds + cue.EndSeconds) / 2)!.Value;
            if (paragraph.Text.Substring(cue.Offset, cue.Length) != original.Passage.Text.Substring(original.Cue.Offset, original.Cue.Length) ||
                cue.StartSeconds != original.Cue.StartSeconds || cue.EndSeconds != original.Cue.EndSeconds || !originalCues.Remove(cue.Id))
                throw new Exception("Paragraph layout changed source words or timing: " + cue.Id);
        }
    if (originalCues.Count != 0) throw new Exception("Paragraph layout lost cues");
    Console.WriteLine($"PASS reading layout: {loaded.Passages.Count} sentence rows → {layout.Passages.Count} reading blocks; all words and source timestamps preserved");
    var count = 0;
    foreach(var passage in layout.Passages)
        foreach(var cue in passage.Cues)
        {
            if(layout.Find(cue.SourceIndex,(cue.StartSeconds+cue.EndSeconds)/2)?.Cue.Id!=cue.Id)
                throw new Exception("Real book lookup failed at " + cue.Id);
            var caption = ReadAlongExcerpt.Create(passage, cue, 150);
            if (caption.Passage.Text != passage.Text[caption.Start..caption.End] ||
                caption.Passage.Text.Substring(caption.ActiveCue.Offset,caption.ActiveCue.Length) != passage.Text.Substring(cue.Offset,cue.Length) ||
                caption.ActiveCue.SourceIndex != cue.SourceIndex || caption.ActiveCue.StartSeconds != cue.StartSeconds)
                throw new Exception("Mini-player caption mapping failed at " + cue.Id);
            count++;
        }
    Console.WriteLine($"PASS real book: {loaded.Passages.Count} passages, {count} cues, source/EPUB fingerprints verified, all cue lookups and mini-player excerpts passed");
    if (args.Length == 3)
    {
        // Optional recovery tool: publish only after the exact app loader validates
        // the current attachment as well, without modifying the library database.
        var store = new ReadAlongStore(Path.GetFullPath(args[2]));
        var saved = await store.LoadAsync(actual.AudiobookId) ?? throw new InvalidDataException("No existing EPUB attachment.");
        var proposed = new BookAssociation { EpubPath = saved.EpubPath, MapPath = Path.GetFullPath(args[1]) };
        await store.LoadMapAsync(actual.AudiobookId, proposed, actual.Sources.Select(s=>s.Path).ToArray());
        saved.MapPath = proposed.MapPath; saved.Status = "ready"; saved.Error = null; saved.DiagnosticPath = null;
        await store.SaveAsync(actual.AudiobookId, saved);
        Console.WriteLine("Published verified alignment to the existing book attachment; library database unchanged.");
    }
    return;
}

var checks = 0;
void Check(bool result, string name) { if (!result) throw new Exception(name); checks++; Console.WriteLine("PASS " + name); }
void Reject(Action action, string name)
{
    try { action(); } catch (InvalidDataException) { Check(true, name); return; }
    throw new Exception("Accepted invalid map: " + name);
}
Check(AudiobookImportSelection.Parse(["book.M4A", "book.EPUB"]) == new AudiobookImportSelection("book.M4A", "book.EPUB"), "import audio and EPUB together, preserving original paths");
Check(AudiobookImportSelection.Parse(["book.mp3"]).EpubPath is null, "audio-only import remains supported");
Check(AudiobookImportSelection.Parse(["book.epub", "book.m4b"]).AudioPath == "book.m4b", "EPUB selection order does not affect audio import");
Reject(() => AudiobookImportSelection.Parse(["book.epub"]), "EPUB without audio rejected before import");
Reject(() => AudiobookImportSelection.Parse(["a.mp3", "b.mp3", "book.epub"]), "multiple audio files directed to existing ordered import");
Reject(() => AudiobookImportSelection.Parse(["a.mp3", "a.epub", "b.epub"]), "ambiguous EPUB attachment rejected");
Reject(() => AudiobookImportSelection.Parse(["a.mp3", "a.txt"]), "unsupported selection rejected before import");
Check(AlignmentProgress.TryParse("PROGRESS:{\"stage\":\"Analysing audio\",\"fraction\":0.42,\"detail\":\"CPU\"}", out var progress) && progress?.Fraction == 0.42, "measured stage progress");
Check(AlignmentProgress.TryParse("PROGRESS:{\"stage\":\"Checking files\"}", out progress) && progress?.Fraction is null, "unknown duration stays indeterminate");
Check(!AlignmentProgress.TryParse("PROGRESS:{\"stage\":\"Audio\",\"fraction\":2}", out _), "invalid progress refused");
Check(!AlignmentProgress.TryParse("PROGRESS:{", out _), "malformed progress ignored");
Check(!AlignmentProgress.TryParse("arbitrary log line", out _), "logs do not invent progress");
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
Check(map.FindContext(0, 3)?.Cue.Id == "a" && map.Find(0, 3) is null, "pause keeps nearby text without inventing a spoken word");
Check(map.FindContext(0, 10)?.Cue.Id == "a", "paragraph pause keeps reading context");
Check(map.FindContext(0, 40) is null, "long unmatched audio does not retain unrelated text");
Check(map.FindContext(1, 0)?.Cue.Id == "b", "source transition shows its own upcoming text");
Check(map.FindContext(20, 1) is null && map.FindContext(0, double.NaN) is null && map.FindContext(0, -1) is null, "unknown and invalid context never uses another source");
var prefs = new ReadingPreferences { FontSize = double.NaN, MiniFontSize = 999, TextColor = "not a colour", HighlightColor = "#123456", HighlightTextColor = "#12345Z", CaptionOpacity = -1 }.Normalize();
Check(prefs.FontSize == 22 && prefs.MiniFontSize == 36 && prefs.CaptionOpacity == 0.4, "appearance sizes and opacity remain usable");
Check(prefs.TextColor is null && prefs.HighlightColor == "#123456" && prefs.HighlightTextColor == "#000000", "invalid saved colours fall back safely");
var persistedPrefs = JsonSerializer.Deserialize<ReadingPreferences>(JsonSerializer.Serialize(prefs))!.Normalize();
Check(persistedPrefs == prefs, "reading appearance survives save and reload");
Check(!CaptionGap.IsSignificant(0,1.00,1.00,0,1.02),"mini status ignores a 20 ms inter-word gap");
Check(!CaptionGap.IsSignificant(0,1.02,1.00,0,1.02),"pausing in an inter-word gap does not create a warning");
Check(!CaptionGap.IsSignificant(0,1.30,1.00,0,1.40),"short narration pauses retain captions");
Check(CaptionGap.IsSignificant(0,2.49,1.00,0,2.50),"genuine unmatched stretch clears stale captions");
Check(CaptionGap.IsSignificant(0,1.00,1.00,1,1.02),"changing source clears an unmatched old caption");
Check(CaptionGap.IsSignificant(0,1.00,1.00,0,100.00),"seeking into unmatched audio clears stale captions immediately");
Check(CaptionGap.IsSignificant(0,100.00,100.00,0,1.00),"backward seek clears stale captions immediately");
Check(map.Find(0,2.02) is null,"gap grace never fabricates an active word highlight");
var longText=string.Concat(Enumerable.Repeat("alpha ",50))+"🙂 lastword";
var captionPassage=new Passage {Id="captions",Text=longText,Cues=Enumerable.Range(0,50).Select(i=>new Cue {Id="w"+i,SourceIndex=0,Offset=i*6,Length=5,StartSeconds=i,EndSeconds=i+0.5,Quality=99}).ToList()};
captionPassage.Cues.Add(new Cue {Id="emoji",SourceIndex=1,Offset=300,Length=2,StartSeconds=0,EndSeconds=1,Quality=99});
foreach(var limit in new[]{32,90,150,220})
{
    foreach(var cue in captionPassage.Cues)
    {
        var caption=ReadAlongExcerpt.Create(captionPassage,cue,limit);
        if(caption.Passage.Text!=longText[caption.Start..caption.End] || caption.ActiveCue.Offset<0 ||
           caption.Passage.Text.Substring(caption.ActiveCue.Offset,caption.ActiveCue.Length)!=longText.Substring(cue.Offset,cue.Length) ||
           caption.Passage.Cues.Any(c=>c.SourceIndex!=cue.SourceIndex)) throw new Exception("Invalid caption slice");
    }
    Check(true,$"mini captions preserve exact text, Unicode, offsets and track mapping at size {limit}");
}
var firstCaption=ReadAlongExcerpt.Create(captionPassage,captionPassage.Cues[0],150);
var secondCaption=ReadAlongExcerpt.Create(captionPassage,captionPassage.Cues[1],150);
Check(firstCaption.Passage.Id==secondCaption.Passage.Id,"mini caption window stays stable between adjacent words");
Check(firstCaption.ActiveCue.StartSeconds==captionPassage.Cues[0].StartSeconds,"clicking mini caption keeps exact source-local seek time");
var longWord=new Passage {Id="long",Text=new string('a',100)};
var longCue=new Cue {Id="longword",Offset=0,Length=100,SourceIndex=0,StartSeconds=0,EndSeconds=1,Quality=99};longWord.Cues.Add(longCue);
Check(ReadAlongExcerpt.Create(longWord,longCue,32).Passage.Text==longWord.Text,"a long spoken word is never cut off");
var orphan = new Passage { Id = "tail", Text = string.Concat(Enumerable.Repeat("word ", 30)) + "last" };
var tailCue = new Cue { Id = "last", Offset = 150, Length = 4, SourceIndex = 0, StartSeconds = 0, EndSeconds = 1, Quality = 99 }; orphan.Cues.Add(tailCue);
Check(ReadAlongExcerpt.Create(orphan, tailCue, 150).Passage.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length > 3, "last word is not stranded on its own caption");
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
    var layoutEpub = Path.Combine(temporary, "paragraphs.epub");
    using (var archive = System.IO.Compression.ZipFile.Open(layoutEpub, System.IO.Compression.ZipArchiveMode.Create))
    {
        await using var writer = new StreamWriter(archive.CreateEntry("EPUB/chapter.xhtml").Open());
        await writer.WriteAsync("<html xmlns='http://www.w3.org/1999/xhtml'><body><p>Hello world.\n Next <em>sentence.</em></p><p>Hello world.</p></body></html>");
    }
    var paragraphMap = Fixture(); paragraphMap.Sources = [paragraphMap.Sources[0]];
    paragraphMap.Epub = await Fingerprint.CreateAsync(layoutEpub);
    paragraphMap.Passages = [
        new() { Id = "s1", Href = "chapter.xhtml", Text = "Hello world.", Cues = [new() { Id = "first", Offset = 0, Length = 5, SourceIndex = 0, StartSeconds = 0, EndSeconds = 1, Quality = 99 }] },
        new() { Id = "s2", Href = "chapter.xhtml", Text = "Next sentence.", Cues = [new() { Id = "next", Offset = 5, Length = 9, SourceIndex = 0, StartSeconds = 3, EndSeconds = 4, Quality = 99 }] },
        new() { Id = "s3", Href = "chapter.xhtml", Text = "Hello world.", Cues = [new() { Id = "repeat", Offset = 0, Length = 5, SourceIndex = 0, StartSeconds = 5, EndSeconds = 6, Quality = 99 }] }];
    paragraphMap.Validate(id, 1);
    var paragraphs = await ReadingLayout.CreateAsync(paragraphMap);
    Check(paragraphs.Passages.Count == 2 && paragraphs.Passages[0].Text == "Hello world.\n Next sentence.", "reader restores exact EPUB paragraphs and inline text");
    var nextCue = paragraphs.Find(0, 3.5)!.Value;
    Check(nextCue.Passage.Text.Substring(nextCue.Cue.Offset, nextCue.Cue.Length) == "sentence." && nextCue.Cue.StartSeconds == 3, "paragraph grouping preserves word offsets and seek times");
    Check(paragraphs.Find(0, 5.5)!.Value.Passage == paragraphs.Passages[1], "repeated text maps to the correct later paragraph");
    Check(paragraphMap.Passages.Count == 3 && paragraphMap.Passages[1].Cues[0].Offset == 5, "presentation layout leaves saved source map untouched");
    Check(ReferenceEquals(await ReadingLayout.CreateAsync(paragraphs), paragraphs), "shared reading layout is reused without parsing again");
    paragraphMap.Passages[1].Text = "Unrecognised sentence.";
    var fallback = await ReadingLayout.CreateAsync(paragraphMap);
    Check(fallback.Passages.Count == 3 && fallback.Passages[0] == paragraphMap.Passages[0], "unusual EPUB structure safely keeps the entire original chapter layout");
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
    await File.WriteAllTextAsync(Path.Combine(fakeEngine, "align.mjs"), "import fs from 'node:fs'; const job=JSON.parse(fs.readFileSync(process.argv[2],'utf8')); console.log('DEVICE:'+job.device); console.log('worker-ready'); setInterval(() => {}, 1000);");
    using var inFlight = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    var workerStarted = false;
    var gpuRequested = false;
    var cancelProgress = new ImmediateProgress(text => { if (text == "DEVICE:dml") gpuRequested=true; if (text == "worker-ready") { workerStarted = true; inFlight.Cancel(); } });
    try
    {
        await new StorytellerProcessor().GenerateAsync(fakeEngine, Path.Combine(temporary,"jobs"), id, epub, [audio], "en", cancelProgress, inFlight.Token, "dml");
        throw new Exception("Running worker was not cancelled");
    }
    catch (OperationCanceledException)
    {
        Check(workerStarted, "cancellation exercised a running worker");
        Check(gpuRequested, "GPU choice forwarded to future worker request");
        Check(!Directory.EnumerateFiles(Path.Combine(temporary,"jobs"), "*.job.json").Any(), "running cancellation cleans up request");
    }
    await File.WriteAllTextAsync(Path.Combine(fakeEngine, "align.mjs"), "console.error('████░░ 10%'); console.error('ERROR:'+JSON.stringify({message:'Échec — EPUB 2'})); process.exitCode=1;");
    try
    {
        await new StorytellerProcessor().GenerateAsync(fakeEngine,Path.Combine(temporary,"jobs"),id,epub,[audio],"en",new Progress<string>(),CancellationToken.None);
        throw new Exception("Failed worker accepted");
    }
    catch (IOException error)
    {
        Check(error.Message.Contains("Échec — EPUB 2"), "UTF-8 structured error preserved");
        Check(!error.Message.Contains('█'), "download progress excluded from error message");
        Check(Directory.EnumerateFiles(Path.Combine(temporary,"jobs"),"*.log").Any(f=>File.ReadAllText(f).Contains("Échec — EPUB 2")), "full UTF-8 diagnostic log retained");
    }
    var diagnosticFile = Path.Combine(temporary,"diagnostic.log");
    await File.WriteAllTextAsync(diagnosticFile,"Échec — full stack details");
    var report = await FailureReport.CreateAsync("Alignment failed",diagnosticFile,"Stage: matching words");
    Check(report.Contains("Stage: matching words") && report.Contains("Échec — full stack details"),"copyable error includes context and Unicode log");
    Check((await FailureReport.CreateAsync("Alignment failed",Path.Combine(temporary,"missing.log"))).Contains("Unable to read diagnostic log"),"missing diagnostic remains copyable");
    await File.WriteAllTextAsync(diagnosticFile,new string('x',100000));
    Check((await FailureReport.CreateAsync("Alignment failed",diagnosticFile)).Length<67000,"large copied logs are bounded");
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
