using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SherpaOnnx;

namespace Audibly.App.Services.Transcription;

/// <summary>
///     Parakeet TDT via sherpa-onnx — the only class in the app that knows about sherpa.
///     Wraps the OfflineRecognizer and assembles sherpa's subword tokens into words:
///     a token starting with a space begins a new word, every other token (subword tails
///     and punctuation like "." or ",") is appended to the current word. Word end times
///     are derived from the next token's timestamp because sherpa's duration output is
///     unreliable for this model.
/// </summary>
public class SherpaOnnxParakeetBackend : ISpeechToTextBackend
{
    private const int LastWordFallbackMs = 300;
    private const int MaxWordDurationMs = 2000;

    private readonly object _lock = new();
    private OfflineRecognizer? _recognizer;

    public SpeechModelDescriptor Model => SpeechModels.ParakeetTdtV3Int8;

    /// <summary>
    ///     The int8 encoder is ~650 MB; a 32-bit process cannot reliably map it.
    /// </summary>
    public bool IsSupportedOnThisDevice => RuntimeInformation.ProcessArchitecture != Architecture.X86;

    public bool IsLoaded => _recognizer != null;

    public Task EnsureLoadedAsync(string modelDirectory, CancellationToken cancellationToken)
    {
        if (_recognizer != null) return Task.CompletedTask;

        return Task.Run(() =>
        {
            lock (_lock)
            {
                if (_recognizer != null) return;
                cancellationToken.ThrowIfCancellationRequested();

                var config = new OfflineRecognizerConfig();
                config.ModelConfig.NumThreads = ResolveThreadCount();
                config.ModelConfig.Provider = "cpu";
                config.ModelConfig.ModelType = "nemo_transducer";
                config.ModelConfig.Tokens = Path.Combine(modelDirectory, "tokens.txt");
                config.ModelConfig.Transducer.Encoder = Path.Combine(modelDirectory, "encoder.int8.onnx");
                config.ModelConfig.Transducer.Decoder = Path.Combine(modelDirectory, "decoder.int8.onnx");
                config.ModelConfig.Transducer.Joiner = Path.Combine(modelDirectory, "joiner.int8.onnx");
                config.FeatConfig.SampleRate = Model.RequiredSampleRate;

                _recognizer = new OfflineRecognizer(config);
            }
        }, cancellationToken);
    }

    public void Unload()
    {
        lock (_lock)
        {
            _recognizer?.Dispose();
            _recognizer = null;
        }
    }

    /// <summary>
    ///     Decode threads, applied when the model (re)loads: the user's explicit setting if
    ///     any, else all logical processors sharing the last-level cache minus one (keeps a
    ///     real core free even when ProcessorCount includes parked LP-E cores), else a
    ///     conservative formula for systems without cache topology (VMs).
    /// </summary>
    public static int ResolveThreadCount()
    {
        var processorCount = Environment.ProcessorCount;

        var manual = Helpers.UserSettings.TranscriptionThreads;
        if (manual > 0) return Math.Min(manual, processorCount);

        var lastLevelCacheCount = CpuTopology.GetLastLevelCacheLogicalProcessorCount();
        if (lastLevelCacheCount > 0 && lastLevelCacheCount <= processorCount)
            return Math.Max(1, lastLevelCacheCount - 1);

        return Math.Max(1, Math.Max(processorCount - 4, processorCount / 2));
    }

    public TimedWord[] TranscribeWindow(float[] pcm, long windowStartAbsMs)
    {
        lock (_lock)
        {
            if (_recognizer == null)
                throw new InvalidOperationException("Speech model is not loaded.");

            using var stream = _recognizer.CreateStream();
            stream.AcceptWaveform(Model.RequiredSampleRate, pcm);
            _recognizer.Decode(stream);
            var result = stream.Result;

            var windowEndAbsMs = windowStartAbsMs + (long)(pcm.Length * 1000.0 / Model.RequiredSampleRate);
            return AssembleWords(result.Tokens, result.Timestamps, windowStartAbsMs, windowEndAbsMs);
        }
    }

    private static TimedWord[] AssembleWords(string[]? tokens, float[]? timestampsSec,
        long windowStartAbsMs, long windowEndAbsMs)
    {
        if (tokens == null || timestampsSec == null || tokens.Length == 0)
            return [];

        var count = Math.Min(tokens.Length, timestampsSec.Length);
        var words = new List<TimedWord>();
        string? currentText = null;
        long currentStartMs = 0;

        for (var i = 0; i < count; i++)
        {
            var token = tokens[i];
            if (string.IsNullOrEmpty(token)) continue;

            var tokenStartMs = windowStartAbsMs + (long)(timestampsSec[i] * 1000);
            var startsNewWord = token[0] == ' ' || currentText == null;

            if (startsNewWord && currentText != null)
            {
                words.Add(CreateWord(currentText, currentStartMs, tokenStartMs, windowEndAbsMs));
                currentText = null;
            }

            if (currentText == null)
            {
                var text = token.TrimStart();
                if (text.Length == 0) continue;
                currentText = text;
                currentStartMs = tokenStartMs;
            }
            else
            {
                currentText += token;
            }
        }

        if (currentText != null)
            words.Add(CreateWord(currentText, currentStartMs, currentStartMs + LastWordFallbackMs, windowEndAbsMs));

        return words.ToArray();
    }

    private static TimedWord CreateWord(string text, long startMs, long nextStartMs, long windowEndAbsMs)
    {
        var endMs = Math.Min(Math.Min(nextStartMs, startMs + MaxWordDurationMs), windowEndAbsMs);
        if (endMs <= startMs) endMs = startMs + 1;
        return new TimedWord(text, (int)startMs, (int)endMs);
    }

    public void Dispose()
    {
        Unload();
    }
}
