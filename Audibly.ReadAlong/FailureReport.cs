using System.Text;

namespace Audibly.ReadAlong;

public static class FailureReport
{
    public static async Task<string> CreateAsync(string summary, string? diagnosticPath, string? context = null)
    {
        var report = new StringBuilder("Audibly Read Along failure\n");
        if (!string.IsNullOrWhiteSpace(context)) report.AppendLine(context);
        report.AppendLine(summary);
        if (diagnosticPath is not null)
        {
            report.AppendLine("Diagnostic log: " + diagnosticPath);
            try
            {
                await using var stream = File.OpenRead(diagnosticPath);
                // Bound clipboard memory; logs can include many download progress lines.
                const int limit = 64 * 1024;
                if (stream.Length > limit) { stream.Seek(-limit, SeekOrigin.End); report.AppendLine("[Last 64 KiB of log]"); }
                using var reader = new StreamReader(stream, Encoding.UTF8);
                report.AppendLine(await reader.ReadToEndAsync());
            }
            catch (IOException e) { report.AppendLine("Unable to read diagnostic log: " + e.Message); }
            catch (UnauthorizedAccessException e) { report.AppendLine("Unable to read diagnostic log: " + e.Message); }
        }
        return report.ToString();
    }
}
