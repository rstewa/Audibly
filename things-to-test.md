# Things to Test

## All Features of Audibly

- [ ] Playback speed (main player, mini-player and settings): the slider and keyboard controls use 1x, 1.1x, 1.25x, 1.5x, 1.75x, 2x, 2.5x, 3x, 3.5x and 4x; tooltips show the speed and voice pitch stays the same.
- [ ] Change speed while playing and paused, including rapid slider changes; retain the current passage and play/pause state.
- [ ] Seek and skip at non-default speeds; chapter titles, progress and saved resume position stay on the original timeline.
- [ ] Reach the end of one file in a multi-file audiobook; the next file starts automatically at the saved speed.
- [ ] Reopen the app/book; resume at the saved position and speed. Check keyboard/media-key play/pause and the sleep timer.
- [ ] Test mono/stereo MP3 and M4B files, including the final words at the end of each file.
- [ ] Mini-player: long titles stay above the smaller controls, with author/chapter text underneath the title. Check light/dark themes and 100%, 150% and 200% display scaling.
- [ ] Mini-player speed button: show the usual icon at 1x and the exact number at every other preset, including 1.25 and 1.75. Verify slider, keyboard and restored-book changes update it immediately.
- [ ] Mini-player sleep timer: show the countdown while active, update each second, and hide it after cancellation or expiry. Check the end-of-chapter timer and hour-long durations; controls must not shift or overlap.
- [ ] Mini-player progress bar: chapter time left on the left, book time left on the right, adjusted for speed. Check seeking while paused, chapter/file transitions, a restored book, and light/dark themes; clicking through either label must still seek.
- [ ] End-of-chapter sleep timer in both players: countdown reflects current speed and adjusts when speed changes; pausing freezes it. Verify it stops at the selected chapter boundary, including the last chapter in a file, without auto-playing the next file. Cancelling or choosing a fixed-duration timer must replace the chapter timer.

- [ ] Filter
  - [ ] In Progress
  - [ ] Not Started
  - [ ] Completed
- [ ] Search
- [ ] Import
  - [ ] Import single audiobook
    - [ ] .m4b
    - [ ] .mp3
    - [ ] broken .m4b
    - [ ] broken .mp3
  - [ ] Import all audiobooks in directory recursively
    - [ ] .m4b only
    - [ ] .mp3 only
    - [ ] mix of .m4b and .mp3
    - [ ] mix of .m4b and .mp3 with broken files
  - [ ] Import audiobook made up of multiple files
    - [ ] .m4b
    - [ ] .mp3
    - [ ] mix of .m4b and .mp3
    - [ ] mix of .m4b and .mp3 with broken files
  - [ ] Import audiobook with no metadata
    - [ ] .m4b
    - [ ] .mp3
  - [ ] Import audiobooks from an Audibly export file
    - [ ] audibly_export.audibly (.json)
- [ ] Export
