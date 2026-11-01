using System.Collections.Concurrent;
using Audibly.App.Services.Audio;
using NAudio.Wave;
using Xunit;
using Windows.Media.Playback;

namespace Audibly.Audio.Tests;

public sealed class PitchPreservingPlayerTests : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"audibly-pitch-test-{Guid.NewGuid()}.wav");
    private readonly ConcurrentQueue<Action> _callbacks = new();
    private readonly PitchPreservingPlayer _player;

    public PitchPreservingPlayerTests()
    {
        WaveFileWriter.CreateWaveFile16(_file, new TempoSampleProviderTests.ToneProvider(12, 2));
        _player = new PitchPreservingPlayer(action => _callbacks.Enqueue(action));
        _player.MediaPlayer.IsMuted = true;
        _player.MediaPlayer.CommandManager.IsEnabled = false;
        _player.MediaPlayer.MediaFailed += (_, e) =>
            _callbacks.Enqueue(() => throw new InvalidOperationException($"{e.Error}: {e.ErrorMessage}"));
    }

    [Fact]
    public async Task SpeedChangesKeepOriginalPositionAndPausedState()
    {
        _player.Open(_file, TimeSpan.FromSeconds(4));
        await Until(() => !_player.IsOpening && Near(_player.Position.TotalSeconds, 4));
        foreach (var speed in new[] { 2.0, 0.5, 4.0, 1.0 })
        {
            _player.Speed = speed;
            await Until(() => !_player.IsOpening && Near(_player.Position.TotalSeconds, 4));
            Assert.Equal(1.0, _player.MediaPlayer.PlaybackSession.PlaybackRate);
            Assert.InRange(_player.Duration.TotalSeconds, 11.99, 12.01);
            Assert.NotEqual(MediaPlaybackState.Playing, _player.MediaPlayer.PlaybackSession.PlaybackState);
            Assert.InRange(_player.MediaPlayer.PlaybackSession.NaturalDuration.TotalSeconds, 12 / speed - 0.02, 12 / speed + 0.02);
        }
        _player.Position = TimeSpan.FromSeconds(8);
        await Until(() => Near(_player.Position.TotalSeconds, 8));
    }

    [Fact]
    public async Task RapidChangesAndSeekWhileOpeningUseLatestSpeedAndPosition()
    {
        _player.Open(_file, TimeSpan.FromSeconds(2));
        _player.Speed = 1.5;
        _player.Speed = 4;
        _player.Speed = 2;
        _player.Position = TimeSpan.FromSeconds(7);
        _player.Play();
        _player.Pause();
        await Until(() => !_player.IsOpening && Near(_player.Position.TotalSeconds, 7));
        Assert.Equal(2, _player.Speed);
        Assert.NotEqual(MediaPlaybackState.Playing, _player.MediaPlayer.PlaybackSession.PlaybackState);
    }

    [Fact]
    public async Task SpeedChangeDuringSeekKeepsRequestedPosition()
    {
        _player.Open(_file, TimeSpan.Zero);
        await Until(() => !_player.IsOpening);
        _player.Position = TimeSpan.FromSeconds(8);
        _player.Speed = 2;
        await Until(() => !_player.IsOpening && Near(_player.Position.TotalSeconds, 8));
        await Until(() => Near(_player.MediaPlayer.PlaybackSession.Position.TotalSeconds, 4));
        _player.Position = TimeSpan.FromSeconds(-1);
        await Until(() => Near(_player.MediaPlayer.PlaybackSession.Position.TotalSeconds, 0));
        _player.Play();
        await Until(() => _player.Position.TotalSeconds > 0.2);
    }

    [Fact]
    public async Task PlaybackResumesAtNewSpeedAndEndsAfterFinalSamples()
    {
        var ended = false;
        _player.MediaPlayer.MediaEnded += (_, _) => ended = true;
        _player.Open(_file, TimeSpan.FromSeconds(9), true);
        await Until(() => _player.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing);
        _player.Speed = 2;
        await Until(() => !_player.IsOpening && _player.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing);
        await Until(() => ended);
        Assert.InRange(_player.Position.TotalSeconds, 11.8, 12.1);

        _player.Open(_file, TimeSpan.FromSeconds(3));
        await Until(() => !_player.IsOpening && Near(_player.Position.TotalSeconds, 3));
        Assert.Equal(2, _player.Speed);
    }

    [Theory]
    [InlineData(".mp3")]
    [InlineData(".m4b")]
    public async Task CompressedAudiobooksDecodeSeekAndPreservePitch(string extension)
    {
        var encodedFile = Path.ChangeExtension(_file, extension == ".m4b" ? ".m4a" : extension);
        var audiobookFile = Path.ChangeExtension(_file, extension);
        try
        {
            NAudio.MediaFoundation.MediaFoundationApi.Startup();
            using (var input = new WaveFileReader(_file))
            {
                if (extension == ".mp3") MediaFoundationEncoder.EncodeToMp3(input, encodedFile, 128000);
                else MediaFoundationEncoder.EncodeToAac(input, encodedFile, 128000);
            }
            if (encodedFile != audiobookFile) File.Move(encodedFile, audiobookFile);

            using (var reader = new MediaFoundationReader(audiobookFile))
            {
                var samples = TempoSampleProviderTests.Drain(new TempoSampleProvider(reader.ToSampleProvider(), 2));
                var format = reader.WaveFormat;
                Assert.InRange(samples.Length / (double)(format.SampleRate * format.Channels), 5.9, 6.1);
                Assert.InRange(TempoSampleProviderTests.MeasureFrequency(samples, format.Channels, 0, format.SampleRate), 437, 443);
            }

            _player.Speed = 2;
            _player.Open(audiobookFile, TimeSpan.FromSeconds(4));
            await Until(() => !_player.IsOpening && Near(_player.Position.TotalSeconds, 4));
            _player.Position = TimeSpan.FromSeconds(8);
            await Until(() => Near(_player.Position.TotalSeconds, 8));
        }
        finally
        {
            _player.Close();
            File.Delete(audiobookFile);
            if (encodedFile != audiobookFile) File.Delete(encodedFile);
        }
    }

    private static bool Near(double actual, double expected) => Math.Abs(actual - expected) < 0.1;

    private async Task Until(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            while (_callbacks.TryDequeue(out var callback)) callback();
            if (condition()) return;
            await Task.Delay(20);
        }
        Assert.Fail($"Timed out. Opening={_player.IsOpening}, position={_player.Position}, state={_player.MediaPlayer.PlaybackSession.PlaybackState}");
    }

    public void Dispose()
    {
        _player.Dispose();
        File.Delete(_file);
    }
}
