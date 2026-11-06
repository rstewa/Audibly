# EPUB Read Along

Implemented from upstream `rstewa/Audibly` commit `ff1d5cfe4656cf6ef01ee0bc59ca6072ea31d279`, on local branch `feature/epub-read-along`. No existing fork was assumed and no hosted fork or PR was created.

## Current status

The Windows x64 application builds. The real CPU alignment pipeline, persisted map, C# subprocess adapter, matching and deliberately mismatched books, and M4B/M4A/multiple-MP3 inputs have been tested. The isolated QA application's native library screen was observed rendering. Computer use was then stopped by the user before the sample import, so Read Along rendering and the complete native interaction workflow remain **unverified**. At the user's request no further computer-use inspection was performed. The relocated bottom-player toggle builds successfully and the QA app has been refreshed. Do not treat this as a release-ready, manually validated feature.

## Setup on Windows 11 x64

Install Node.js 24 LTS (including npm), .NET SDK 8.0.4xx or newer, and Visual Studio 2022's Windows application development tools/Windows SDK if not already installed. No Python, CUDA toolkit, Storyteller server, cloud account, or API subscription is needed.

In PowerShell, from the repository root:

```powershell
npm.cmd ci --prefix tools/read-along
dotnet build Audibly.App/Audibly.App.csproj -p:Platform=x64 -p:RuntimeIdentifier=win-x64
```

The engine dependencies are installed into `tools/read-along/node_modules`; the lockfile pins the tested dependency tree. `allowScripts` in package.json permits the required native dependency installation scripts. FFmpeg, FFprobe and a local Node 24 runtime are provided by those dependencies, without editing system PATH. First alignment downloads the QuartzNet model to `%LOCALAPPDATA%\ghost-story\models` (roughly 182 MB for the downloaded model variants). Later processing works offline with those cached dependencies. Allow disk space for a temporary copy of each source audio file and emissions, as well as the EPUB and map.

Launch the separate development build from PowerShell:

```powershell
cd D:\audibly-transcript
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Start-ReadAlong.ps1
```

The launcher builds and registers only `Audibly.ReadAlong.Development`, displayed in Start as **Audibly Read Along QA**. It refreshes `.research/qa-app`, gracefully closes only that checkout's QA process, and keeps its library/settings across launches. It refuses to replace a development registration from another directory and removes audiobook file associations from the QA manifest. Your installed Audibly retains its original identity and data. Use `-NoBuild` to register/launch an already-built version. Do not deploy the original production-identity manifest directly from Visual Studio when testing alongside the Store application.

`NuGet.Config` adds the official Toolkit Labs feed needed by Audibly's existing package versions. The project pins Windows SDK projection `10.0.19041.56`, which supplies the WinRT.Runtime 2.2 required by those packages on older .NET SDKs. These resolve baseline build problems; they do not upgrade the player framework.

## Use

1. Import an M4B, M4A or MP3 audiobook using Audibly's existing import commands. Use its existing multi-file import for a split audiobook, in the intended source order.
2. Click **Read Along** beside the play/skip buttons in the bottom playback bar. It opens the expanded player and reading panel. The same toggle is available beside the expanded player's play/skip buttons; it no longer sits in the window's top-right area.
3. Choose **Engine folder…** and select this checkout's `tools/read-along` folder. This selection is remembered. The folder must contain `align.mjs` and the installed dependencies.
4. Choose **Attach EPUB**, select the matching DRM-free ebook, and set the audio language (`en` by default).
5. Choose **Generate alignment**. Progress appears in the panel. **Cancel** terminates the worker process tree. Completed emissions can be reused when retrying.
6. Play using the normal controls. Reliable words are highlighted in gold using the player's source position. Click an aligned word to seek. Untimed text stays readable and unhighlighted.
7. Scroll manually to suspend following; choose **Follow narration** to resume. Toggle Read Along off to return to the original cover/player view. The toggle is remembered.

Attaching an EPUB creates a managed copy; neither the original EPUB nor audiobook is modified. A book without an EPUB continues through the original player code. Reading saved maps does not launch Node or require Storyteller.

## Technology verified

The actual npm stable package inspected and executed was `@storyteller-platform/align@0.2.16`, requiring Node >=24. Both its CLI (`dist/cli/bin.js --help`) and shipped implementation were inspected. Its CTC `buildWordRanges` produces individual word start/end positions, and `createMediaOverlay` emits word ID-fragment references in nested SMIL sequences. This is actual word timing, not a sentence timing map presented as words.

The adapter uses the supported exported APIs `emit`, `markup` and `ctcAlign` in a separate Node process. QuartzNet CPU emissions are matched against the ebook's text. It does not run Whisper, store an ASR transcript for display, or copy the alignment algorithm into C#.

Processing steps:

1. Stream SHA-256 fingerprints of the original EPUB and all source files, preserving Audibly's source order.
2. Create an atomic, byte-for-byte staged copy of each source, with unique numeric filenames. Run `emit` directly on these **unsplit, untranscoded** files. Do not invoke `processAudiobook`.
3. Mark up a separate EPUB copy with word/sentence spans. Run CTC alignment to a temporary EPUB with Media Overlays and a version 4 JSON quality report.
4. Parse actual SMIL ID references and match those IDs to the marked EPUB. Assert that original and marked EPUB body text is identical. Extract published text and UTF-16 offsets from that verified EPUB DOM.
5. Accept only `storyteller:matched` cues whose ancestors are also free of interpolated/unmatched/dropped/audio-only states, with a reported chapter quality score >=80. The threshold is a conservative policy, not a calibrated probability of correctness.
6. Map the staged audio filename back to the original Audibly source index. SMIL seconds remain local to that exact source file. Reject unknown audio tracks rather than guess offsets. Refingerprint inputs before publishing a map atomically.

The timestamp contract avoids segmentation-offset ambiguity: chapter offsets and cumulative book time are never added. A two-file test intentionally used filenames in a different alphabetical order from the requested playback order; staging preserved the order. FFmpeg may decode audio internally for inference, but no split/transcoded output replaces or supplies the playback timeline.

Sources reviewed: [standalone alignment announcement](https://storyteller-platform.dev/blog/20260305_stalign/), [CTC/QuartzNet discussion](https://storyteller-platform.dev/blog/20260920_quartznet/), [published npm package](https://www.npmjs.com/package/@storyteller-platform/align), and the package's shipped README, declarations and JavaScript. The downloaded package, rather than older announcement prose, determined the integration contract.

## Storage and playback architecture

`Audibly.ReadAlong` is a dependency-free .NET 8 library containing the versioned map, validation, source-local binary-search lookup, persistent attachment store and cancellable process adapter. `Audibly.App` references it and provides the native controls.

Data lives under Audibly's existing `ApplicationData.Current.LocalFolder`, beside `Audibly.db`:

```text
ReadAlong/<existing audiobook GUID>/
  book.json                    EPUB association, map path, status/error
  <attachment GUID>.epub       managed unchanged copy
  cache/<input/settings hash>/
    alignment.json             versioned timing map and exact EPUB text
    audio/                     unchanged source copies
    emissions/                 reusable completed emission files
    marked.epub                generated IDs and unchanged published text
    report.json                upstream quality report
```

The association uses the existing database audiobook identifier. It is a sidecar rather than an EF schema change, keeping optional preprocessing failures and large payloads outside library rows. Library exports/deletion do not yet include/clean these sidecars. Reattaching retains the previous managed copy/cache; deleting the book's ReadAlong folder reclaims them.

The cache key includes all full input hashes, input order/paths, language, schema and engine version. Maps are revalidated and source fingerprints checked before use when the book is opened. Hashing runs off the UI thread. Corrupt, stale, wrong-book, out-of-range, overlapping and low-quality maps are rejected. No missing timestamps are invented. Input mutation during processing aborts publication. Partial staged audio is never reused; upstream emissions use partial files and completed identity records.

`PlayerViewModel` remains the owner of the **only** MediaPlayer. The panel polls its source-local position every 50 ms, without integrating wall-clock time or playback rate. Cross-file word clicks use the existing source-opening path and apply the pending source-local seek after `MediaOpened`, preserving playing/paused state. Existing chapter skipping, progress, bookmarks and playback controls are retained.

Rendering uses a virtualized ListView of sentence passages, with native RichTextBlock/Hyperlink inlines only for realized rows. TextHighlighter uses the stored UTF-16 word offset. Auto-follow scrolls when the current sentence changes, not on every word. Wheel, pointer, keyboard and touch manipulation suspend following. No WebView2 or second playback engine is introduced.

## Reproduce verification

From repository root:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/read-along/create-sample.ps1
& tools/read-along/node_modules/node/bin/node.exe tools/read-along/poc.mjs
& tools/read-along/node_modules/node/bin/node.exe tools/read-along/verify.mjs
& tools/read-along/node_modules/node/bin/node.exe tools/read-along/verify-formats.mjs
$mapFile = (Get-ChildItem tools/read-along/sample/cache -Recurse -Filter alignment.json | Select-Object -First 1).FullName
dotnet run --project Audibly.ReadAlong.Tests -- $mapFile tools/read-along
```

The sample is original CC0 prose spoken by Windows' installed System.Speech voice, with a matching generated EPUB. Generated sample audio, ebooks and caches are gitignored; no copyrighted audiobook is redistributed. `create-sample.ps1` needs Windows PowerShell 5.1 and an installed speech voice, but these are test-only dependencies.

Actual results on 6 November 2026:

| Verification | Result |
|---|---|
| Matching 71.38-second WAV + EPUB, CPU | 202 word cues; 17 matched spoken sentences; unspoken heading dropped |
| First words | “The” 0.060–0.260 s; “morning” 0.280–0.600 s |
| Published text preservation | Original and marked EPUB body text identical |
| Deliberately unrelated EPUB, same audio/emissions | Zero accepted cues |
| Sentence mode through actual engine/SMIL | 17 cues explicitly labelled `sentence` |
| Interpolated ancestor, low quality, unknown audio track | Suppressed/rejected |
| Actual M4B, M4A, two MP3 files | 202 cues each; MP3 cues mapped to both source-local timelines |
| Cache rerun | Reused same map without rewriting it |
| C# core + real map + real subprocess adapter | 228 assertions passed |
| Windows x64 app build | Succeeded; existing upstream warnings remain |
| Isolated packaged app launch | Succeeded |
| Native visual/click/playback integration | Not verified; computer-use app approval timed out |

The playback-rate assertions exercise the position-based lookup contract, not live playback at multiple speeds. Actual pause/seek/chapter/speed controls, clicking, scrolling, long-book responsiveness, cancellation during a large inference operation, book switching and app restart/progress restoration still need native manual testing. See `read-along-evidence.json` for captured alignment totals.

## Limits and remaining work

- Windows x64 and English QuartzNet CPU are the verified configuration. Other language/model combinations and native ARM64 dependencies are unverified. The actual engine supports `cpu`, `webgpu`, Windows `dml`, `cuda`, and Apple `mlx` options in appropriate environments, but this adapter deliberately selects CPU; it exposes no unverified GPU setting.
- Edited/abridged books, repeated passages, music, advertisements and edition differences depend on the upstream matcher. It skips unsupported matches; no policy can guarantee every accepted word is correct. Native quality review/editing is not implemented.
- A word cut across source files follows upstream behavior: its timing stays on the source where it begins. Sentence fallback may have multiple source-local clips.
- The converter understands the verified 0.2.16 ID-fragment SMIL and seconds clock format. Future schema changes fail explicitly. EPUB2 auto-upgrade and experimental text-fragment output are not enabled. Unsupported/encrypted EPUBs fail without modifying the library.
- Text is preserved, but ebook typography, images, footnote navigation and rich inline formatting are not reproduced. This is a narration panel, not a full ebook renderer. Text appears after successful processing; attaching alone shows status rather than an unaligned ebook preview.
- The full timing model is held in memory, but UI controls are virtualized. Very long books and unusually large individual sentences need memory/UI profiling. Full-file validation hashes can take time on multi-hour books.
- Maps and attachments are retained locally. Cache cleanup, detaching controls, portable export/import and emissions-only reuse across a changed EPUB are future improvements.
- The alignment quality cutoff is chapter-level, with upstream matched-word filtering; no calibrated per-word confidence is available in the exported SMIL.

## Licences

Audibly remains GPL-3.0. The alignment package is MIT, `ghost-story@0.3.4` is GPL-3.0, ONNX Runtime is MIT, and the FFmpeg static dependency carries GPL-3.0. The code licences are compatible with this GPL application. They are separately installed local dependencies; do not publish bundled binaries without retaining their notices and satisfying applicable source obligations. The lockfile includes upstream dependency metadata and URLs.

Model weights have their own terms. The automatically downloaded Storyteller QuartzNet conversion is **not bundled here**. NVIDIA's original [QuartzNet model page](https://catalog.ngc.nvidia.com/orgs/nvidia/nemo/models/stt_en_quartznet15x5) points to NGC terms; other conversions advertise different licences. A redistribution grant for Storyteller's exact converted weights has not been established, so this work makes no blanket model-redistribution compatibility claim. Review those terms before distributing weights. This does not require a cloud inference service for local processing.

## Changed files

- `Audibly.ReadAlong/{AlignmentMap,ReadAlongStore,StorytellerProcessor}.cs` and project: independent processing/storage/timing core.
- `Audibly.ReadAlong.Tests/{Program.cs,Audibly.ReadAlong.Tests.csproj}`: executable automated checks without test-framework dependencies.
- `Audibly.App/UserControls/ReadAlongPanel.xaml[.cs]`, `ReadAlongPassage.cs`: native panel and realized passage rendering.
- `Audibly.App/Views/PlayerPage.xaml[.cs]`: toggle and remembered panel visibility.
- `Audibly.App/ViewModels/PlayerViewModel.cs`: source-aware click-to-seek using the existing player.
- `Audibly.App/{Services/FileImportService.cs,ViewModels/MainViewModel.cs,Views/LibraryCardPage.xaml.cs,Package.appxmanifest}`: M4A import support/labels/association.
- `Audibly.App/Audibly.App.csproj`, `Audibly.sln`, `NuGet.Config`: project references and baseline build fixes.
- `tools/read-along/{align,convert,poc,verify,verify-formats}.mjs`, `create-sample.ps1`, package manifest/lock: pinned local engine adapter and real validation fixtures.
- `.gitignore`, `docs/read-along*`: generated-output exclusions, setup, evidence and limitations.
