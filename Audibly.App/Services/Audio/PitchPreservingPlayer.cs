using System;
using Windows.Media.Playback;

namespace Audibly.App.Services.Audio;

/// <summary>Owns the audio source and translates between audiobook and output time.</summary>
public sealed class PitchPreservingPlayer : IDisposable
{
    private readonly Action<Action> _dispatch;
    private PitchPreservingMediaSource? _source;
    private TimeSpan _pendingPosition;
    private bool _resumeAfterOpen;
    private bool _isSeeking;
    private bool _disposed;
    private double _speed = 1.0;

    public PitchPreservingPlayer(Action<Action> dispatch)
    {
        _dispatch = dispatch;
        MediaPlayer.AutoPlay = false;
        MediaPlayer.MediaOpened += OnMediaOpened;
        MediaPlayer.PlaybackSession.SeekCompleted += OnSeekCompleted;
        MediaPlayer.CommandManager.PlayReceived += OnPlayReceived;
        MediaPlayer.CommandManager.PauseReceived += OnPauseReceived;
    }

    public MediaPlayer MediaPlayer { get; } = new();
    public bool IsOpening { get; private set; }
    public TimeSpan Duration => _source?.Duration ?? TimeSpan.Zero;

    public TimeSpan Position
    {
        get => IsOpening || _isSeeking ? _pendingPosition :
            _source?.ToSourceTime(MediaPlayer.PlaybackSession.Position) ?? TimeSpan.Zero;
        set
        {
            _pendingPosition = TimeSpan.FromTicks(Math.Clamp(value.Ticks, 0, Duration.Ticks));
            if (!IsOpening && _source != null)
            {
                var target = _source.ToPlaybackTime(_pendingPosition);
                _isSeeking = _isSeeking || Math.Abs((MediaPlayer.PlaybackSession.Position - target).TotalMilliseconds) > 1;
                MediaPlayer.PlaybackSession.Position = target;
            }
        }
    }

    public double Speed
    {
        get => _speed;
        set
        {
            if (!double.IsFinite(value) || value < 0.5 || value > 4.0)
                throw new ArgumentOutOfRangeException(nameof(value));
            if (_speed == value) return;

            // Reopen at the audible position, not the decoder's buffered-ahead position.
            if (_source != null)
                ReplaceSource(_source.FilePath, value, Position, IsOpening ? _resumeAfterOpen :
                    MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing);
            _speed = value;
        }
    }

    public void Open(string filePath, TimeSpan position, bool autoPlay = false) =>
        ReplaceSource(filePath, _speed, position, autoPlay);

    private void ReplaceSource(string filePath, double speed, TimeSpan position, bool autoPlay)
    {
        var replacement = new PitchPreservingMediaSource(filePath, speed);
        var previous = _source;
        MediaPlayer.Pause();
        IsOpening = true;
        _isSeeking = false;
        _source = replacement;
        _pendingPosition = TimeSpan.FromTicks(Math.Clamp(position.Ticks, 0, replacement.Duration.Ticks));
        _resumeAfterOpen = autoPlay;
        // SoundTouch changes tempo. Applying PlaybackRate as well would change pitch again.
        MediaPlayer.PlaybackSession.PlaybackRate = 1.0;
        MediaPlayer.Source = replacement.Source;
        previous?.Dispose();
    }

    private void OnMediaOpened(MediaPlayer sender, object args)
    {
        var openedSource = sender.Source;
        _dispatch(() =>
        {
            if (_disposed || _source == null || !ReferenceEquals(openedSource, _source.Source)) return;
            IsOpening = false;
            Position = _pendingPosition;
            if (_resumeAfterOpen) MediaPlayer.Play();
        });
    }

    private void OnSeekCompleted(MediaPlaybackSession sender, object args)
    {
        var source = _source;
        var position = source?.ToSourceTime(sender.Position) ?? TimeSpan.Zero;
        _dispatch(() =>
        {
            if (!_disposed && ReferenceEquals(source, _source) &&
                Math.Abs((position - _pendingPosition).TotalMilliseconds) < 100)
                _isSeeking = false;
        });
    }

    private void OnPlayReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPlayReceivedEventArgs args)
    {
        args.Handled = true;
        _dispatch(() => { if (!_disposed) Play(); });
    }

    private void OnPauseReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPauseReceivedEventArgs args)
    {
        args.Handled = true;
        _dispatch(() => { if (!_disposed) Pause(); });
    }

    public void Play()
    {
        _resumeAfterOpen = true;
        if (!IsOpening) MediaPlayer.Play();
    }

    public void Pause()
    {
        _resumeAfterOpen = false;
        MediaPlayer.Pause();
    }

    public void Close()
    {
        Pause();
        IsOpening = false;
        _isSeeking = false;
        var previous = _source;
        _source = null;
        MediaPlayer.Source = null;
        previous?.Dispose();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        MediaPlayer.MediaOpened -= OnMediaOpened;
        MediaPlayer.PlaybackSession.SeekCompleted -= OnSeekCompleted;
        MediaPlayer.CommandManager.PlayReceived -= OnPlayReceived;
        MediaPlayer.CommandManager.PauseReceived -= OnPauseReceived;
        Close();
        MediaPlayer.Dispose();
    }
}
