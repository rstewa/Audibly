using Audibly.App.Services.Audio;
using NAudio.Wave;
using Xunit;

namespace Audibly.Audio.Tests;

public class TempoSampleProviderTests
{
    [Theory]
    [InlineData(0.5, 1)]
    [InlineData(0.75, 2)]
    [InlineData(1.0, 1)]
    [InlineData(1.25, 2)]
    [InlineData(1.5, 1)]
    [InlineData(2.0, 2)]
    [InlineData(3.0, 1)]
    [InlineData(4.0, 2)]
    public void ChangesDurationButPreservesPitch(double speed, int channels)
    {
        var input = new ToneProvider(4, channels);
        var provider = new TempoSampleProvider(input, speed);
        var output = Drain(provider);

        Assert.InRange(output.Length / (double)(input.WaveFormat.SampleRate * channels), 4 / speed - 0.01, 4 / speed + 0.01);
        for (var channel = 0; channel < channels; channel++)
        {
            // Measure the waveform, not just the configured SoundTouch parameters.
            // Different frequencies on left/right also catch interleaving mistakes.
            var frequency = MeasureFrequency(output, channels, channel, input.WaveFormat.SampleRate);
            Assert.InRange(frequency, ToneProvider.Frequency(channel) - 3, ToneProvider.Frequency(channel) + 3);
        }
    }

    [Fact]
    public void NormalSpeedLeavesSamplesUnchanged()
    {
        Assert.Equal(Drain(new ToneProvider(1, 2)), Drain(new TempoSampleProvider(new ToneProvider(1, 2), 1)));
    }

    [Fact]
    public void ResetDiscardsBufferedAudioAndAllowsReadingAfterEnd()
    {
        var input = new ToneProvider(2, 2);
        var provider = new TempoSampleProvider(input, 2);
        var scratch = new float[400];
        provider.Read(scratch, 0, scratch.Length);
        input.Frame = 0;
        provider.Reset();
        var expected = Drain(new TempoSampleProvider(new ToneProvider(2, 2), 2));
        Assert.Equal(expected, Drain(provider));
        input.Frame = 0;
        provider.Reset();
        Assert.Equal(expected, Drain(provider));
    }

    [Fact]
    public void FinalShortBufferIsDrainedAndEndOfStreamStaysEmpty()
    {
        var provider = new TempoSampleProvider(new ToneProvider(0.037, 1), 1.5);
        var output = Drain(provider);
        Assert.InRange(output.Length, 1086, 1089);
        Assert.Equal(0, provider.Read(new float[512], 0, 512));
    }

    internal static float[] Drain(ISampleProvider source)
    {
        var result = new List<float>();
        var buffer = new float[1024 * source.WaveFormat.Channels + 8];
        int count;
        while ((count = source.Read(buffer, 4, buffer.Length - 8)) > 0)
            result.AddRange(buffer.AsSpan(4, count).ToArray());
        return result.ToArray();
    }

    internal static double MeasureFrequency(float[] samples, int channels, int channel, int sampleRate)
    {
        var first = sampleRate / 5;
        var last = samples.Length / channels - sampleRate / 5;
        var crossings = new List<double>();
        for (var frame = first + 1; frame < last; frame++)
        {
            var previous = samples[(frame - 1) * channels + channel];
            var current = samples[frame * channels + channel];
            if (previous <= 0 && current > 0)
                crossings.Add(frame - 1 - previous / (current - previous));
        }
        return (crossings.Count - 1) * sampleRate / (crossings[^1] - crossings[0]);
    }

    internal sealed class ToneProvider(double seconds, int channels) : ISampleProvider
    {
        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(44100, channels);
        public int Frame { get; set; }
        public static double Frequency(int channel) => channel == 0 ? 440 : 660;

        public int Read(float[] buffer, int offset, int count)
        {
            var frames = Math.Min(count / channels, (int)(seconds * WaveFormat.SampleRate) - Frame);
            for (var i = 0; i < frames; i++)
                for (var channel = 0; channel < channels; channel++)
                    buffer[offset + i * channels + channel] =
                        (float)(0.3 * Math.Sin(2 * Math.PI * Frequency(channel) * (Frame + i) / WaveFormat.SampleRate));
            Frame += frames;
            return frames * channels;
        }
    }
}
