# Read Along UI audit — 6 November 2026

Scope: source/UI review, Windows compilation, synthetic regressions and validation against the two existing QA books. No computer-use inspection or native interaction automation was performed, as requested. The results below do not establish visual correctness or audible synchronization.

## Findings corrected

| Finding | Change | Evidence |
|---|---|---|
| Each sentence became a separate reader row, producing excessive gaps and isolated snippets | Reconstruct actual EPUB paragraphs for presentation, preserving exact text/whitespace and cue offsets. Fall back to the original chapter layout if matching is not exact | Synthetic inline markup, repeated paragraphs and fallback tests; all 213,295 real cues retain exact words and timestamps |
| Mini-player replaced text after only 1.5 seconds without an active cue | Show nearby context without highlighting during normal pauses; show an unmatched hint only beyond nearby source-local cues | Pause, seek, source-transition and distant-unmatched tests. Confidence Gap has 782 gaps over 1.5 seconds, only one over 12 seconds |
| A trailing word could become a caption by itself | Balance caption boundaries across the paragraph | A regression reproduced the orphaned-word case; it now passes. All real-book caption offsets/seek times validated |
| Clickable words could use link colours; reading/highlight colours were fixed | Bind links to reading foreground; add native Aa appearance flyout with selectable text/highlight colours, separate main/mini font sizes, opacity and reset | Windows XAML/C# build; saved preferences normalization and reload tests |
| Negative control spacing overlapped mini-player hit areas | Use 32-pixel compact buttons, normal spacing and a flexible title column | XAML review/build; native sizing/hit-testing remains unverified |
| Rebuilding caption excerpts on every spoken word added unnecessary allocations | Reuse visible excerpts, reset scrolling on replacement, skip hidden/minimized updates | Existing and real-book excerpt checks; native performance has not been profiled |
| Long paragraphs or large fonts could leave the active word outside the visible area | Request minimal scrolling to the active word while following; allow extra mini-player height at the largest text sizes | Windows build; actual scrolling and large-font fit remain unverified |

## Validation

- Windows x64 app builds successfully, with existing upstream warnings.
- C# regression suite: 275 checks pass.
- First QA book: 8,275 sentence rows become 3,277 reading blocks; 140,076 word cues preserved.
- The Confidence Gap: 5,073 sentence rows become 2,061 reading blocks; 73,219 word cues preserved.
- EPUB/audio fingerprints, exact cue text, every cue lookup, and every mini caption excerpt pass for both books.
- No speech analysis was repeated and no original EPUB, audio or persisted map was rewritten.

## Remaining limits

- Native colour-picker interactions, light/dark appearance, control fit, font extremes, DPI scaling, drag/pin, clicking and audible synchronization still need a visual/manual check. They are not verified by compilation or model tests.
- Genuinely unmatched audio and suppressed unreliable cues remain unhighlighted. The Confidence Gap contains a roughly 61-second gap between accepted cues; changing presentation cannot supply missing alignment.
- Books with unusual EPUB structure may retain sentence rows in the affected chapter. Real EPUB paragraph grouping substantially reduced rows for both tested books, but does not guarantee reconstruction for every publisher layout.
- Preparation remains tied to the main reader panel. Leaving/unloading that panel during preparation can cancel its worker. This existing lifecycle limitation was not redesigned in this UI audit.
- Opacity controls the caption backing within the app window; the mini-player is not a separately transparent desktop overlay.

## Follow-up — 19 November 2026

The initial layout left setup actions, language/device options and technical status text on the prepared reader. The revised UI uses a compact title/filename toolbar with Aa and an overflow settings flyout. Ebook setup is a centred empty state; prepared books show the text immediately. Progress and errors appear only while relevant. Colour swatches replace the always-expanded colour picker, and background opacity is only shown in the mini-player. A three-row player grid replaces fixed overlay margins and hides the cover behind the reader.

The main book button no longer restores an old enabled flag at app startup. It reflects the actual expanded reading state. Paired import, the toolbar and the mini-player now use one navigation entry point that sets fullscreen state, glyph and tooltip before opening the player. Page lifecycle updates keep Escape/back/maximize consistent.

Mini-player reading now uses the complete saved paragraph list with native virtualization. Manual scrolling, scrollbar dragging, touch panning and navigation keys pause following; audio continues unchanged. A Follow narration button resumes live text. Browsing disables word-seek links and renders one run per visible paragraph, preventing accidental rewinds and avoiding a control per word in history. Playback ticks update only realized rows and never scroll while browsing.

The review's cross-file seek race is covered by a source-open coordinator: loaded media is distinct from the selected/opening source, repeated clicks replace the pending seek, stale asynchronous source work is rejected, and only the matching open consumes the latest request. The C# suite now passes 285 checks. A real Windows MediaPlayer smoke check also verified assigned-source identity at MediaOpened without playing audio.

The clean-checkout build workflow installs Node 24 and locked npm dependencies before MSBuild, runs the core regressions, allows unsigned validation when signing secrets are absent, and uses supported artifact upload actions. Publish output has an explicit directory and targets only the application. An isolated source export with no node_modules passed npm installation and an unsigned Release MSIX packaging build; these local checks do not imply a completed GitHub-hosted workflow run.

Native visual appearance, touch/trackpad scroll feel and live UI interactions remain unverified through computer use, which the user has prohibited. The source/layout review and native build are complete; they do not substitute for visual interaction checks. Existing protected library storage and saved timing maps are preserved during replacement of the local app.
