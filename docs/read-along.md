# EPUB Read Along

Implemented from upstream `rstewa/Audibly` commit `ff1d5cfe4656cf6ef01ee0bc59ca6072ea31d279`, on branch `feature/epub-read-along`. The contribution is published in the `saucedasf/Audibly` fork for upstream review.

## Current status

The Windows x64 application builds. Real CPU and DirectML GPU alignment, the persisted map, C# subprocess adapter, matching and deliberately mismatched books, and M4B/M4A/multiple-MP3 inputs have been tested. The isolated QA application's native library screen was observed rendering earlier. Computer use was then stopped by the user before the sample import, so Read Along rendering and the complete native interaction workflow remain **unverified**. At the user's request no further computer-use inspection was performed. Paired audio/EPUB import, the main book icon and GPU-preferred preparation compile, and the QA app has been refreshed. Do not treat this as a release-ready, manually validated feature.

## Setup on Windows 11 x64

Install Node.js 24 LTS (including npm), .NET SDK 8.0.4xx or newer, and Visual Studio 2022's Windows application development tools/Windows SDK if not already installed. No Python, CUDA toolkit, Storyteller server, cloud account, or API subscription is needed.

In PowerShell, from the repository root:

```powershell
npm.cmd ci --prefix tools/read-along
dotnet build Audibly.App/Audibly.App.csproj -p:Platform=x64 -p:RuntimeIdentifier=win-x64
```

For developers, `npm ci` prepares dependencies in `tools/read-along/node_modules`; the lockfile pins the tested dependency tree. The build includes the adapter, Node 24, FFmpeg, FFprobe, Storyteller, native inference libraries and dependency notices under the app's `read-along` directory, including in publish/package content. Users do not need a system Node/FFmpeg installation or an engine-folder selection. The panel always uses the bundled copy and ignores the old folder preference. `allowScripts` in package.json permits the required developer dependency installation scripts. First alignment automatically downloads the QuartzNet model to `%LOCALAPPDATA%\ghost-story\models` (roughly 182 MB for the downloaded model variants). Later processing works offline with those cached dependencies. Allow disk space for a temporary copy of each source audio file and emissions, as well as the EPUB and map.

Launch the separate development build from PowerShell:

```powershell
cd D:\audibly-transcript
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Start-ReadAlong.ps1
```

The launcher builds and registers only `Audibly.ReadAlong.Development`, displayed in Start as **Audibly**. It refreshes `.research/qa-app`, gracefully closes only the modified app, and keeps its library/settings across launches. Its private package identity remains different from the Store application even though the visible name is Audibly. It refuses unexpected registration paths and removes file associations from the development manifest. Your installed Audibly retains its original identity and data. Use `-NoBuild` to register/launch an already-built version. Do not deploy the original production-identity manifest directly from Visual Studio when testing alongside the Store application.

For a local installation independent of the checkout, run `Install-ReadAlong.ps1` once (or `Install-ReadAlong.ps1 -NoBuild` after building). This copies the bundled app to `%LOCALAPPDATA%\Programs\AudiblyReadAlong`, registers that location using the same isolated identity, and creates an **Audibly** desktop shortcut using `Assets\logo.ico`. Existing modified-app library/settings stay in the same Windows package storage. Subsequent launches use the shortcut or Start menu, without PowerShell. The installer refuses to interrupt active alignment or force-close an app that will not shut down gracefully. It preserves the previous checkout payload. Later developer-launcher updates recognize the installed location and refresh it there.

This local installation uses a registered development package; it is not yet a signed consumer MSIX/Store release. It relies on the existing Windows/.NET application runtimes supplied by the development setup, while Node/FFmpeg and alignment dependencies are copied with the app. A desktop shortcut that already launches another Audibly installation is preserved, with **Audibly (Read Along)** used for the new shortcut in that case.

Windows icon assets are copied explicitly to build/publish output, and the executable embeds `Assets\logo.ico`. Main and mini windows resolve their icon from the application's absolute install directory. The installer checks required icon/tile assets before registration. A missing-icon regression was reproduced through Windows shell icon extraction: before repair the app returned a blank-document icon; after restoring the missing image assets and refreshing the shell it returned Audibly's waveform icon. This check verifies shell icon resolution, not the rest of the native UI.

`NuGet.Config` adds the official Toolkit Labs feed needed by Audibly's existing package versions. The project pins Windows SDK projection `10.0.19041.56`, which supplies the WinRT.Runtime 2.2 required by those packages on older .NET SDKs. These resolve baseline build problems; they do not upgrade the player framework.

## Use

1. Choose **Import an audiobook + optional EPUB**. Select one M4B, M4A or MP3 and, optionally, its matching EPUB together (Ctrl-click both files). The EPUB is copied into the imported book's own storage and the reading panel opens ready for **Prepare Read Along**. Audio-only import still works. Use the existing multi-file import for a split audiobook, in the intended source order, then attach its EPUB from the reading panel.
2. The **book icon beside volume and playback speed** opens Read Along from the bottom playback bar and toggles the panel in the expanded player. The large text button beside play/skip has been removed.
3. Choose **Choose EPUB and prepare** and select the matching DRM-free ebook. Preparation starts automatically. The selected filename stays visible; **Change EPUB** lets you replace it. If an EPUB was already attached, the main action is **Prepare Read Along**. Audio language (`en` by default) is under **Processing options**.
4. A persistent preparation card shows the current stage, a progress bar, elapsed time and **Cancel preparation**. Percentages refer to the current stage, not an invented overall estimate. Stages without measured progress show an indeterminate bar. Completed emissions can be reused when retrying.
5. Play using the normal controls. Reliable words are highlighted in gold using the player's source position. Click an aligned word to seek. Untimed text stays readable and unhighlighted.
6. Scroll manually to suspend following; choose **Follow narration** to resume. Toggle Read Along off to return to the original cover/player view. The toggle is remembered.

Attaching an EPUB creates a managed copy; neither the original EPUB nor audiobook is modified. A book without an EPUB continues through the original player code. Reading saved maps does not launch Node or require Storyteller.

## Floating mini-player captions

Open the existing **Open Mini Player** button from the player's controls. The mini-player has a **book icon beside volume, speed and timer** to toggle captions; when enabled, the current EPUB excerpt appears **above** the controls on a dark `#DD181818` backing with the active word highlighted. There is no separate text-labelled Read Along button under the words. The caption pane and controls are a single movable window. Use the existing drag-grip icon next to the cover to move them together. **Pin** keeps the window above other windows; the grip remains usable while pinned. This is a dark-backed caption panel, not a separate transparent desktop window.

Use **A− / A+** to change the caption text size (remembered between launches). **Aa** in both readers opens native appearance controls for text size, reading-text colour, highlight background, highlighted-text colour and mini-player background opacity. Colours are shared; main and mini text sizes are independent. **Reset appearance** restores theme-aware text, gold/black highlighting and the dark caption backing. Excerpts are balanced exact substrings of the EPUB so a final word does not occupy a caption by itself. Aligned words remain clickable for seeking. Pause, speed and chapter controls use the same original MediaPlayer. The mini-player does not start analysis or generate text. If no saved map is available, **Open Read Along** returns to setup in the main player.

Normal playback does not alternate “Following narration” and “No aligned words here.” Nearby source-local text remains visible through sentence/paragraph pauses, without a highlighted word. Context is limited to 12 seconds from an actual cue; seeking or changing sources looks up text at the new position immediately. Only genuinely unaligned stretches without nearby context show the unmatched-position hint. Context never extends word timestamps. Font controls disappear with captions, leaving the book icon among the compact player controls.

Both readers reconstruct the original EPUB paragraphs for display instead of rendering every sentence as a separate row. Published text, whitespace, word identity and exact source-local cue times are preserved. The persisted map and EPUB are untouched; an unusual chapter that cannot be mapped exactly retains its original sentence layout. Line height is 1.5 times the chosen text size, with spacing between paragraphs. Clickable words use the reading-text colour instead of the default hyperlink colour. The mini-player uses non-overlapping compact controls, reuses the current excerpt while its words remain visible, resets scrolling for a new excerpt and avoids caption updates while hidden/minimized.

The full and mini views share the already-validated alignment document for the current book. The mini-player can also load an existing saved map asynchronously without opening the full panel first. Hiding captions returns to a shorter mini-player and does not affect audio. The caption toggle and font size are independent remembered mini-player preferences.

Automated caption tests cover exact text preservation, Unicode, offsets, stable excerpt windows, separate source files, long words and original seek times. The C# suite passes 245 checks. The production loader additionally verified every caption excerpt and lookup against the user's 140,076-cue map without redoing speech analysis. The native WinUI build passes. Floating-window appearance, drag/pin behavior and actual caption clicks have not been computer-use tested, per the user's instruction not to use that skill.

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
| Bundled application runtime, system Node/FFmpeg removed from PATH | Real M4A CPU alignment succeeded: 202 word cues |
| C# adapter using the app-bundled engine | 228 assertions passed |
| Windows x64 app build | Succeeded; existing upstream warnings remain |
| Isolated packaged app launch | Succeeded |
| Native visual/click/playback integration | Not verified; computer-use app approval timed out |

The playback-rate assertions exercise the position-based lookup contract, not live playback at multiple speeds. Actual pause/seek/chapter/speed controls, clicking, scrolling, long-book responsiveness, cancellation during a large inference operation, book switching and app restart/progress restoration still need native manual testing. See `read-along-evidence.json` for captured alignment totals. The subsequent one-click opening fix, simplified preparation flow and structured progress card compile. The launcher refuses to update/restart while a QA alignment worker is active.

### EPUB 2 compatibility and readable failures

The user's failed run finished audio analysis, then failed because the attached ebook was EPUB 2. The adapter now inspects the ebook before audio hashing/analysis and uses the maintained upstream `Epub.upgrade` on a separate byte copy. Every original reading-order document's href and exact body text must match after conversion. The original attachment and its fingerprint remain the cache identity, so completed CPU emissions from the failed run remain reusable.

The user's actual 78-chapter ebook passed EPUB 2-to-3 conversion with identical chapter body text and an untouched original. A legal EPUB 2 sample produced 202 timing cues from existing emissions, and a broken EPUB was rejected before attempting to open audio. Run `tools/read-along/verify-epub2.mjs` using the local Node executable to reproduce the synthetic regression. No audio inference of the user's book was repeated during these checks.

Worker stdout/stderr are decoded explicitly as UTF-8. A structured failure message is displayed without terminal download bars or stack traces; full stderr is retained as a UTF-8 `.job.log` in the book's cache folder for diagnosis. The subsequent lightweight C# suite passed 235 checks, including Unicode errors, clean summaries and retained diagnostic logs. Previous EPUB-2 failure messages are replaced in the panel with a readable retry instruction.

### Protected Windows cache regression and copyable diagnostics

The next full-book failure was reproduced as a Windows `copyfile` UNKNOWN error: `marked.epub` had the Encrypted attribute in packaged LocalState, and upstream attempted to copy it into unencrypted user TEMP. A copy within the same protected cache succeeded. The adapter now sets the upstream-supported `STORYTELLER_SCRATCH_DIR` to `<book cache>/scratch` before any EPUB work, retaining scratch files in the same protected storage. No encryption settings or source-book files are changed.

`verify-protected-cache.mjs <directory inside QA LocalState>` reproduces that environment with the legal sample and existing emissions. The regression passed with an encrypted marked EPUB, 202 cues and unchanged emissions; it forbids new inference. The actual user's book then completed matching from its existing analysis cache. Full-book validation exposed overlapping upstream cue ranges; the converter now removes every connected ambiguous overlap group without modifying the published text or inventing replacement timing. Legacy maps are migrated atomically in place, keeping the expensive emissions/cache identity. Nested/chained intervals, touching boundaries, separate tracks and idempotent migration are covered by `verify-ambiguity.mjs`.

The recovered full-book map passed the production C# loader, EPUB/audio SHA-256 checks and every lookup midpoint: 8,275 passages and 140,076 usable cues. A total of 217 ambiguous cues were suppressed and their text remains unhighlighted. These are automated backend/loader checks, not a claim of native visual or audible synchronization verification. No speech inference of the user's book was repeated.

Error text is selectable. **Copy error details** copies the summary, processing context and UTF-8 diagnostic log (last 64 KiB for large logs) to the clipboard. Missing logs still produce a useful report. Failures remain copyable after reopening a book. The diagnostic report builder, Unicode behavior, missing logs and log-size bounds are covered by the C# suite, now 238 checks. The native clipboard-button click itself has not been UI-automated, in accordance with the user's instruction not to use computer use.

For a recovered timing map, the test runner supports `--validate-map <alignment.json>` to exercise the production loader and all lookups. An optional third argument, the QA LocalState directory, associates that map with an existing attachment only after validating its current EPUB and sources again; it does not modify the library database.

## CPU and GPU processing

New preparations default to **GPU preferred (CPU fallback)** under **Processing options**, using Storyteller's Windows DirectML path. CPU can be chosen explicitly. Existing completed maps remain usable without new analysis. An already-running CPU job cannot change processor midway and is never interrupted by the launcher.

On 6 November 2026, after confirming no user alignment worker remained active, a fresh M4A sample completed with `requestedDevice=dml` and `actualDevice=dml`, producing 202 validated word cues. This verifies real GPU model loading/inference, not just a requested setting or cached map. Repeat with `tools/read-along/node_modules/node/bin/node.exe tools/read-along/verify-gpu.mjs` after generating the legal sample. It does not identify which physical adapter DirectML selects or establish a full-book speedup. Audio decoding and text matching still perform CPU work. The Windows build and all 260 C# regression checks passed, including combined-import selection, attachment persistence, cancellation, errors, and cue lookup. Native visual/click verification remains outstanding because computer use was disabled by the user.

The actual shipped engine selects DirectML automatically on Windows when `auto` is requested, while its CUDA selection is gated to Linux x64. Its GPU loader can fall back to CPU if model loading fails. The adapter forwards an explicit `dml` choice only for future GPU requests, labels it preview, forwards fallback notices, and records the actual model-load device when upstream logging exposes it. Runtime failure after model loading is still reported as an error, not silently retried. No CUDA toolkit setup is required for this DirectML option. This is supported by the inspected 0.3.4 package source (`constants.js`, `emission/CtcModel.js`, `emission/ConvCtcOnnx.js`) and [ONNX Runtime's DirectML requirements](https://onnxruntime.ai/docs/execution-providers/DirectML-ExecutionProvider.html).

GPU acceleration may reduce speech-model inference time, but file hashing/copying and text matching also take time. No speedup factor or completion estimate is promised. Existing CPU cache keys remain unchanged so current completed work can be reused. A GPU request has a separate cache identity to keep device-dependent emissions distinct.

## Limits and remaining work

- Windows x64 and English QuartzNet CPU are the verified configuration. GPU/DirectML is a pending opt-in preview requiring an idle-machine benchmark and native verification; CPU remains the default. Other language/model combinations and native ARM64 dependencies are unverified.
- Edited/abridged books, repeated passages, music, advertisements and edition differences depend on the upstream matcher. It skips unsupported matches; no policy can guarantee every accepted word is correct. Native quality review/editing is not implemented.
- A word cut across source files follows upstream behavior: its timing stays on the source where it begins. Sentence fallback may have multiple source-local clips.
- The converter understands the verified 0.2.16 ID-fragment SMIL and seconds clock format. Future schema changes fail explicitly. EPUB 2 is upgraded on a text-verified working copy before audio processing. Experimental text-fragment output is not enabled. Unsupported/encrypted EPUBs fail without modifying the library.
- Text is preserved, but ebook typography, images, footnote navigation and rich inline formatting are not reproduced. This is a narration panel, not a full ebook renderer. Text appears after successful processing; attaching alone shows status rather than an unaligned ebook preview.
- The full timing model is held in memory, but UI controls are virtualized. Very long books and unusually large individual sentences need memory/UI profiling. Full-file validation hashes can take time on multi-hour books.
- Maps and attachments are retained locally. Cache cleanup, detaching controls, portable export/import and emissions-only reuse across a changed EPUB are future improvements.
- The alignment quality cutoff is chapter-level, with upstream matched-word filtering; no calibrated per-word confidence is available in the exported SMIL.

## Licences

Audibly remains GPL-3.0. The alignment package is MIT, `ghost-story@0.3.4` is GPL-3.0, ONNX Runtime is MIT, and the FFmpeg static dependency carries GPL-3.0. The code licences are compatible with this GPL application. Dependency notices are retained in the bundled dependency tree. Before distributing release binaries, satisfy applicable corresponding-source and other redistribution obligations; the current local development bundle is not a completed third-party distribution audit. The lockfile includes upstream dependency metadata and URLs.

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
