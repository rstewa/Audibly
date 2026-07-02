using System;
using System.Threading;
using System.Threading.Tasks;

namespace Audibly.App.Services.Transcription;

/// <summary>
///     One recognized word with absolute times (ms within the current source file).
/// </summary>
public readonly record struct TimedWord(string Text, int StartMs, int EndMs);

/// <summary>
///     Speech-to-text backend abstraction. The transcription pipeline (chunking, stitching,
///     sentence assembly, scheduling, persistence, UI) only ever consumes
///     <see cref="TimedWord" />s and the <see cref="SpeechModelDescriptor" />, so swapping the
///     model or the runtime (e.g. whisper.cpp) means implementing this one interface.
///     Token semantics (subword pieces, word-boundary markers, punctuation attachment) are
///     backend-specific and must not leak past it.
/// </summary>
public interface ISpeechToTextBackend : IDisposable
{
    SpeechModelDescriptor Model { get; }

    bool IsSupportedOnThisDevice { get; }

    bool IsLoaded { get; }

    /// <summary>
    ///     Loads the model from the given directory (expensive; ~seconds and ~1 GB resident).
    /// </summary>
    Task EnsureLoadedAsync(string modelDirectory, CancellationToken cancellationToken);

    /// <summary>
    ///     Frees the loaded model.
    /// </summary>
    void Unload();

    /// <summary>
    ///     Transcribes one window of mono PCM at <see cref="SpeechModelDescriptor.RequiredSampleRate" />.
    ///     Returned word times are absolute: window-relative results shifted by
    ///     <paramref name="windowStartAbsMs" />.
    /// </summary>
    TimedWord[] TranscribeWindow(float[] pcm, long windowStartAbsMs);
}
