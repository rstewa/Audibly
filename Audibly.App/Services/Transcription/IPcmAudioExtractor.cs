using System;
using System.Threading;
using System.Threading.Tasks;

namespace Audibly.App.Services.Transcription;

/// <summary>
///     Raised when a range of audio cannot be decoded to PCM (corrupt/missing file,
///     decoder error, timeout).
/// </summary>
public class PcmExtractionException : Exception
{
    public PcmExtractionException(string message) : base(message)
    {
    }

    public PcmExtractionException(string message, Exception inner) : base(message, inner)
    {
    }
}

/// <summary>
///     A decoded range of audio, readable window-by-window as mono float PCM.
/// </summary>
public interface IPcmAudioSource : IAsyncDisposable
{
    /// <summary>
    ///     Actual decoded duration of the range (can be shorter than requested at file end).
    /// </summary>
    long DurationMs { get; }

    /// <summary>
    ///     Reads one window; offset is relative to the start of the range. The returned
    ///     array is truncated at the end of the range.
    /// </summary>
    Task<float[]> ReadWindowAsync(long offsetMs, long lengthMs, CancellationToken cancellationToken);
}

/// <summary>
///     Decodes arbitrary audiobook audio to mono PCM at a fixed sample rate. This is the
///     seam that isolates the transcription pipeline from the decoder: the default
///     implementation uses LibVLC, but a MediaFoundation/NAudio backend can be swapped in
///     for builds without the VLC player.
/// </summary>
public interface IPcmAudioExtractor
{
    /// <summary>
    ///     False when the decoder's native libraries are unavailable on this device.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    ///     Decodes [startMs, startMs + durationMs) of the file.
    /// </summary>
    Task<IPcmAudioSource> OpenRangeAsync(string filePath, long startMs, long durationMs,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Frees decoder resources held between chapters (re-created on demand).
    /// </summary>
    void Unload();
}
