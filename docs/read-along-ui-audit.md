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
