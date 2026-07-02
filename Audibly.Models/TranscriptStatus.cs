namespace Audibly.Models;

/// <summary>
///     Transcription state of a single chapter.
/// </summary>
public enum TranscriptStatus
{
    NotStarted = 0,
    Queued = 1,
    InProgress = 2,
    Completed = 3,
    Failed = 4
}
