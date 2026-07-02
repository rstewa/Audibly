using System;
using System.Collections.Generic;
using System.Text;
using Audibly.Models;

namespace Audibly.App.Services.Transcription;

/// <summary>
///     A finished sentence with word-level timings, ready to persist.
/// </summary>
public record SentenceDraft(string Text, int StartMs, int EndMs, bool IsParagraphStart, WordTiming[] Words);

/// <summary>
///     Groups a stream of timed words into sentences. Stateful per chapter: an unfinished
///     sentence is carried until a boundary arrives or <see cref="Flush" /> is called.
///     Boundaries come from the model's punctuation; hard caps (350 chars / 30 s) bound
///     every sentence so the packed word-timing codec's ushort fields always fit.
/// </summary>
public class SentenceAssembler
{
    private const int SoftBreakChars = 200;
    private const int HardBreakChars = 350;
    private const int HardBreakMs = 30_000;
    private const int ParagraphGapMs = 1500;

    private static readonly char[] SentenceEnders = ['.', '!', '?', '…', '。', '！', '？'];
    private static readonly char[] ClosingTrail = ['"', '\'', '”', '’', '»', ')', ']'];
    private static readonly char[] SoftBreakers = [',', ';', ':'];

    private readonly List<TimedWord> _words = [];
    private int _charLength;
    private long _lastEmittedEndMs = -1;

    /// <summary>
    ///     Adds words (in time order) and returns any sentences they completed.
    /// </summary>
    public List<SentenceDraft> Push(IEnumerable<TimedWord> words)
    {
        var completed = new List<SentenceDraft>();

        foreach (var word in words)
        {
            if (string.IsNullOrWhiteSpace(word.Text)) continue;

            _words.Add(word);
            _charLength += word.Text.Length + (_words.Count > 1 ? 1 : 0);

            if (IsSentenceEnd(word.Text) ||
                _charLength >= HardBreakChars ||
                word.EndMs - _words[0].StartMs >= HardBreakMs ||
                (_charLength >= SoftBreakChars && IsSoftBreak(word.Text)))
            {
                completed.Add(Emit());
            }
        }

        return completed;
    }

    /// <summary>
    ///     Emits the trailing unfinished sentence (end of chapter), if any.
    /// </summary>
    public SentenceDraft? Flush()
    {
        return _words.Count == 0 ? null : Emit();
    }

    private SentenceDraft Emit()
    {
        var startMs = _words[0].StartMs;
        var endMs = Math.Max(_words[^1].EndMs, startMs + 1);

        var text = new StringBuilder(_charLength);
        var timings = new WordTiming[_words.Count];

        for (var i = 0; i < _words.Count; i++)
        {
            if (i > 0) text.Append(' ');
            var offset = text.Length;
            text.Append(_words[i].Text);

            timings[i] = new WordTiming(
                (ushort)Math.Min(offset, ushort.MaxValue),
                (ushort)Math.Min(_words[i].Text.Length, ushort.MaxValue),
                (ushort)Math.Clamp(_words[i].StartMs - startMs, 0, ushort.MaxValue),
                (ushort)Math.Clamp(_words[i].EndMs - _words[i].StartMs, 1, ushort.MaxValue));
        }

        var isParagraphStart = _lastEmittedEndMs < 0 || startMs - _lastEmittedEndMs >= ParagraphGapMs;
        _lastEmittedEndMs = endMs;

        _words.Clear();
        _charLength = 0;

        return new SentenceDraft(text.ToString(), startMs, endMs, isParagraphStart, timings);
    }

    private static bool IsSentenceEnd(string word)
    {
        var trimmed = word.AsSpan().TrimEnd(ClosingTrail);
        return trimmed.Length > 0 && Array.IndexOf(SentenceEnders, trimmed[^1]) >= 0;
    }

    private static bool IsSoftBreak(string word)
    {
        return word.Length > 0 && Array.IndexOf(SoftBreakers, word[^1]) >= 0;
    }
}
