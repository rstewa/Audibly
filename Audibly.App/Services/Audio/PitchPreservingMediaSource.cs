using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Audibly.App.Services.Audio;

/// <summary>
/// Streams decoded, time-stretched audio to MediaPlayer at its normal playback rate.
/// All public positions are in the original audiobook's timeline.
/// </summary>
internal sealed class PitchPreservingMediaSource : IDisposable
{
    private readonly object _gate = new();
    private readonly MediaFoundationReader _reader;
    private readonly TempoSampleProvider _samples;
    private readonly IWaveProvider _pcm;
    private readonly MediaStreamSource _stream;
    private long _outputFrames;
    private TimeSpan _startPosition;
    private bool _discontinuous = true;
    private bool _disposed;

    public PitchPreservingMediaSource(string filePath, double speed)
    {
        FilePath = filePath;
        Speed = speed;
        _reader = new MediaFoundationReader(filePath);
        try
        {
            Duration = _reader.TotalTime;
            _samples = new TempoSampleProvider(_reader.ToSampleProvider(), speed);
            _pcm = new SampleToWaveProvider16(_samples);
            var format = _pcm.WaveFormat;
            _stream = new MediaStreamSource(new AudioStreamDescriptor(
                AudioEncodingProperties.CreatePcm((uint)format.SampleRate, (uint)format.Channels, 16)))
            {
                CanSeek = true,
                Duration = ToPlaybackTime(Duration),
                BufferTime = TimeSpan.FromMilliseconds(100)
            };
            _stream.Starting += OnStarting;
            _stream.SampleRequested += OnSampleRequested;
            Source = MediaSource.CreateFromMediaStreamSource(_stream);
        }
        catch
        {
            _reader.Dispose();
            throw;
        }
    }

    public string FilePath { get; }
    public double Speed { get; }
    public TimeSpan Duration { get; }
    public MediaSource Source { get; }

    public TimeSpan ToPlaybackTime(TimeSpan position) => TimeSpan.FromTicks((long)(position.Ticks / Speed));
    public TimeSpan ToSourceTime(TimeSpan position) => TimeSpan.FromTicks((long)(position.Ticks * Speed));

    private void OnStarting(MediaStreamSource sender, MediaStreamSourceStartingEventArgs args)
    {
        lock (_gate)
        {
            if (_disposed) return;
            try
            {
                if (args.Request.StartPosition is { } position)
                {
                    var sourcePosition = ToSourceTime(position);
                    _reader.CurrentTime = TimeSpan.FromTicks(Math.Clamp(sourcePosition.Ticks, 0, Duration.Ticks));
                    _samples.Reset();
                    _startPosition = ToPlaybackTime(_reader.CurrentTime);
                    _outputFrames = 0;
                    _discontinuous = true;
                }

                args.Request.SetActualStartPosition(OutputPosition);
            }
            catch
            {
                sender.NotifyError(MediaStreamSourceErrorStatus.Other);
            }
        }
    }

    private TimeSpan OutputPosition => _startPosition +
        TimeSpan.FromTicks(_outputFrames * TimeSpan.TicksPerSecond / _pcm.WaveFormat.SampleRate);

    private void OnSampleRequested(MediaStreamSource sender, MediaStreamSourceSampleRequestedEventArgs args)
    {
        lock (_gate)
        {
            if (_disposed) return;
            try
            {
                var format = _pcm.WaveFormat;
                // Each sample owns its buffer; MediaPlayer may retain it after this callback returns.
                var buffer = new byte[Math.Max(1, format.SampleRate / 50) * format.BlockAlign];
                var read = _pcm.Read(buffer, 0, buffer.Length);
                if (read == 0)
                {
                    args.Request.Sample = null;
                    return;
                }

                var timestamp = OutputPosition;
                _outputFrames += read / format.BlockAlign;
                var sample = MediaStreamSample.CreateFromBuffer(buffer.AsBuffer(0, read), timestamp);
                sample.Duration = OutputPosition - timestamp;
                sample.Discontinuous = _discontinuous;
                _discontinuous = false;
                args.Request.Sample = sample;
            }
            catch
            {
                sender.NotifyError(MediaStreamSourceErrorStatus.Other);
            }
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            _stream.Starting -= OnStarting;
            _stream.SampleRequested -= OnSampleRequested;
            _reader.Dispose();
        }

        Source.Dispose();
    }
}
