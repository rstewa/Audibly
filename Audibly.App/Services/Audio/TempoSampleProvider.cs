using System;
using NAudio.Wave;
using SoundTouch;

namespace Audibly.App.Services.Audio;

/// <summary>Changes tempo without changing pitch or the output sample rate.</summary>
internal sealed class TempoSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly SoundTouchProcessor _processor;
    private readonly float[] _input;
    private readonly bool _passthrough;
    private bool _endOfInput;

    public TempoSampleProvider(ISampleProvider source, double speed)
    {
        if (!double.IsFinite(speed) || speed <= 0)
            throw new ArgumentOutOfRangeException(nameof(speed));

        _source = source;
        _passthrough = speed == 1.0;
        _input = new float[4096 * source.WaveFormat.Channels];
        _processor = new SoundTouchProcessor
        {
            SampleRate = source.WaveFormat.SampleRate,
            Channels = source.WaveFormat.Channels,
            Tempo = speed,
            Pitch = 1.0,
            Rate = 1.0
        };
    }

    public WaveFormat WaveFormat => _source.WaveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        if (_passthrough) return _source.Read(buffer, offset, count);
        var channels = WaveFormat.Channels;
        var written = 0;
        count -= count % channels;

        while (written < count)
        {
            var frames = _processor.ReceiveSamples(buffer.AsSpan(offset + written, count - written),
                (count - written) / channels);
            written += frames * channels;
            if (written == count || _endOfInput) break;

            var read = _source.Read(_input, 0, _input.Length);
            if (read == 0)
            {
                // Drain the final syllable before signalling end of the file.
                _processor.Flush();
                _endOfInput = true;
            }
            else
            {
                _processor.PutSamples(_input.AsSpan(0, read), read / channels);
            }
        }

        return written;
    }

    public void Reset()
    {
        _processor.Clear();
        _endOfInput = false;
    }
}
