namespace Audibly.ReadAlong;

public sealed class AlignmentProcessingException(string message, string diagnosticPath) : IOException(message)
{
    public string DiagnosticPath { get; } = diagnosticPath;
}
