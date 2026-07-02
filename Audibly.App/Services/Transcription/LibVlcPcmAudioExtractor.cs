using System;
using System.Buffers.Binary;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using LibVLCSharp.Shared;

namespace Audibly.App.Services.Transcription;

/// <summary>
///     Decodes audio ranges to 16 kHz mono PCM by running a LibVLC transcode into a
///     temporary WAV file (same recipe as the onnx_feasibility PoC), then serving windows
///     from that file. Uses its own LibVLC instance — never the player's.
/// </summary>
public class LibVlcPcmAudioExtractor : IPcmAudioExtractor
{
    private const int TargetChannels = 1;
    private const int BytesPerSample = 2;

    private readonly int _sampleRate;
    private readonly object _lock = new();
    private LibVLC? _libVlc;

    public LibVlcPcmAudioExtractor(int sampleRate)
    {
        _sampleRate = sampleRate;
    }

    private int BytesPerMs => _sampleRate * TargetChannels * BytesPerSample / 1000;

    public bool IsAvailable
    {
        get
        {
            try
            {
                EnsureLibVlc();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public static string TempDirectory =>
        Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "Transcription");

    /// <summary>
    ///     Deletes WAV files left behind by a previous crash.
    /// </summary>
    public static void SweepTempFiles()
    {
        try
        {
            if (Directory.Exists(TempDirectory)) Directory.Delete(TempDirectory, true);
        }
        catch
        {
            // best effort
        }
    }

    public async Task<IPcmAudioSource> OpenRangeAsync(string filePath, long startMs, long durationMs,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
            throw new PcmExtractionException($"Audio file not found: {filePath}");

        var libVlc = EnsureLibVlc();
        Directory.CreateDirectory(TempDirectory);
        var wavPath = Path.Combine(TempDirectory, $"{Guid.NewGuid():N}.wav");

        try
        {
            await TranscodeAsync(libVlc, filePath, startMs, durationMs, wavPath, cancellationToken);
            return WavPcmSource.Open(wavPath, BytesPerMs);
        }
        catch
        {
            TryDelete(wavPath);
            throw;
        }
    }

    public void Unload()
    {
        lock (_lock)
        {
            _libVlc?.Dispose();
            _libVlc = null;
        }
    }

    private LibVLC EnsureLibVlc()
    {
        lock (_lock)
        {
            return _libVlc ??= new LibVLC("--intf=dummy", "--no-video-title-show");
        }
    }

    private async Task TranscodeAsync(LibVLC libVlc, string inputPath, long startMs, long durationMs,
        string outputPath, CancellationToken cancellationToken)
    {
        var startSeconds = startMs / 1000.0;
        var durationSeconds = durationMs / 1000.0;

        using var media = new Media(libVlc, new Uri(inputPath));
        media.AddOption(":no-video");
        media.AddOption(":sout-keep");
        media.AddOption(":no-sout-video");
        if (startSeconds > 0)
            media.AddOption($":start-time={startSeconds.ToString(CultureInfo.InvariantCulture)}");
        media.AddOption($":run-time={durationSeconds.ToString(CultureInfo.InvariantCulture)}");
        media.AddOption(
            $":sout=#transcode{{acodec=s16l,channels={TargetChannels},samplerate={_sampleRate}}}:std{{access=file,mux=wav,dst='{EscapeVlcPath(outputPath)}'}}");

        using var mediaPlayer = new MediaPlayer(libVlc);
        // RunContinuationsAsynchronously is required: the completion fires on a LibVLC
        // callback thread, and continuing synchronously there would deadlock on Stop().
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnEndReached(object? s, EventArgs e)
        {
            completion.TrySetResult(true);
        }

        void OnError(object? s, EventArgs e)
        {
            completion.TrySetResult(false);
        }

        mediaPlayer.EndReached += OnEndReached;
        mediaPlayer.EncounteredError += OnError;

        try
        {
            if (!mediaPlayer.Play(media))
                throw new PcmExtractionException($"LibVLC refused to start decoding {inputPath}.");

            var timeout = TimeSpan.FromMinutes(2) + TimeSpan.FromMilliseconds(durationMs / 2);
            var finished = await Task.WhenAny(completion.Task,
                Task.Delay(timeout, cancellationToken)) == completion.Task;

            cancellationToken.ThrowIfCancellationRequested();
            if (!finished)
                throw new PcmExtractionException(
                    $"Timed out decoding {Path.GetFileName(inputPath)} after {timeout.TotalSeconds:F0}s.");

            if (!await completion.Task)
                throw new PcmExtractionException($"LibVLC failed to decode {Path.GetFileName(inputPath)}.");
        }
        finally
        {
            mediaPlayer.EndReached -= OnEndReached;
            mediaPlayer.EncounteredError -= OnError;
            mediaPlayer.Stop();
        }
    }

    private static string EscapeVlcPath(string path)
    {
        return path.Replace("'", "\\'");
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // best effort
        }
    }

    /// <summary>
    ///     Serves float PCM windows from the transcoded WAV file.
    /// </summary>
    private sealed class WavPcmSource : IPcmAudioSource
    {
        private readonly FileStream _stream;
        private readonly long _dataOffset;
        private readonly long _dataLength;
        private readonly int _bytesPerMs;
        private readonly string _path;

        private WavPcmSource(FileStream stream, long dataOffset, long dataLength, int bytesPerMs, string path)
        {
            _stream = stream;
            _dataOffset = dataOffset;
            _dataLength = dataLength;
            _bytesPerMs = bytesPerMs;
            _path = path;
        }

        public long DurationMs => _dataLength / _bytesPerMs;

        public static WavPcmSource Open(string path, int bytesPerMs)
        {
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            try
            {
                var (dataOffset, dataLength) = LocateDataChunk(stream);
                return new WavPcmSource(stream, dataOffset, dataLength, bytesPerMs, path);
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }

        public async Task<float[]> ReadWindowAsync(long offsetMs, long lengthMs, CancellationToken cancellationToken)
        {
            var byteOffset = offsetMs * _bytesPerMs;
            if (byteOffset >= _dataLength) return [];

            var byteCount = (int)Math.Min(lengthMs * _bytesPerMs, _dataLength - byteOffset);
            byteCount -= byteCount % BytesPerSample;
            if (byteCount <= 0) return [];

            var bytes = new byte[byteCount];
            _stream.Seek(_dataOffset + byteOffset, SeekOrigin.Begin);
            await _stream.ReadExactlyAsync(bytes, cancellationToken);

            var samples = new float[byteCount / BytesPerSample];
            for (var i = 0; i < samples.Length; i++)
                samples[i] = BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan(i * BytesPerSample)) / 32768f;

            return samples;
        }

        /// <summary>
        ///     Walks the RIFF chunks to find the "data" chunk. VLC's wav muxer may write a
        ///     placeholder data size, so the size is clamped to the actual file length.
        /// </summary>
        private static (long Offset, long Length) LocateDataChunk(FileStream stream)
        {
            Span<byte> header = stackalloc byte[12];
            stream.ReadExactly(header);
            if (!header[..4].SequenceEqual("RIFF"u8) || !header[8..12].SequenceEqual("WAVE"u8))
                throw new PcmExtractionException("Decoded audio is not a RIFF/WAVE file.");

            Span<byte> chunkHeader = stackalloc byte[8];
            while (stream.Position + 8 <= stream.Length)
            {
                stream.ReadExactly(chunkHeader);
                var chunkSize = BinaryPrimitives.ReadUInt32LittleEndian(chunkHeader[4..]);

                if (chunkHeader[..4].SequenceEqual("data"u8))
                {
                    var offset = stream.Position;
                    var available = stream.Length - offset;
                    var length = chunkSize == 0 || chunkSize == 0xFFFFFFFF
                        ? available
                        : Math.Min(chunkSize, available);
                    return (offset, length);
                }

                stream.Seek(chunkSize + (chunkSize % 2), SeekOrigin.Current);
            }

            throw new PcmExtractionException("Decoded WAV file has no data chunk.");
        }

        public ValueTask DisposeAsync()
        {
            _stream.Dispose();
            TryDelete(_path);
            return ValueTask.CompletedTask;
        }
    }
}
