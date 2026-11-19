using System.Diagnostics;
using System.Text.Json;
using System.Text;

namespace Audibly.ReadAlong;

public sealed class StorytellerProcessor
{
    public async Task<string> GenerateAsync(string engineFolder, string cacheFolder, Guid bookId,
        string epub, IReadOnlyList<string> sources, string language, IProgress<string> progress, CancellationToken token,
        string device = "cpu")
    {
        if (!File.Exists(Path.Combine(engineFolder, "align.mjs")))
            throw new FileNotFoundException("Read Along components are missing from this installation. Repair or reinstall Audibly.");
        Directory.CreateDirectory(cacheFolder);
        var jobFile = Path.Combine(cacheFolder, Guid.NewGuid() + ".job.json");
        var logFile = Path.ChangeExtension(jobFile, ".log");
        try
        {
            await File.WriteAllTextAsync(jobFile, JsonSerializer.Serialize(new
            { audiobookId = bookId, epub, sources, language, device, cache = cacheFolder }, AlignmentMap.Json), token);
            var localNode = Path.Combine(engineFolder, "node_modules", "node", "bin", "node.exe");
            var start = new ProcessStartInfo(File.Exists(localNode) ? localNode : "node.exe")
            {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true,
                RedirectStandardError = true, WorkingDirectory = engineFolder,
                StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
            };
            start.Environment["NO_COLOR"] = "1";
            start.Environment["FORCE_COLOR"] = "0";
            start.Environment["TERM"] = "dumb";
            start.ArgumentList.Add(Path.Combine(engineFolder, "align.mjs"));
            start.ArgumentList.Add(jobFile);
            using var process = Process.Start(start) ?? throw new IOException("Unable to start local alignment engine.");
            using var registration = token.Register(() =>
            {
                try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) { }
                catch (System.ComponentModel.Win32Exception) { }
            });
            string? result = null;
            string? failure = null;
            await using var diagnostics = new StreamWriter(logFile, false, new UTF8Encoding(false));
            async Task ReadOutput()
            {
                while (await process.StandardOutput.ReadLineAsync() is { } line)
                    if (line.StartsWith("RESULT:", StringComparison.Ordinal)) result = line[7..];
                    else progress.Report(line.StartsWith("PROGRESS:", StringComparison.Ordinal) ? line : line.Length > 240 ? line[..240] : line);
            }
            async Task ReadErrors()
            {
                while (await process.StandardError.ReadLineAsync() is { } line)
                {
                    await diagnostics.WriteLineAsync(line);
                    if (line.StartsWith("ERROR:", StringComparison.Ordinal))
                    {
                        try
                        {
                            using var document = JsonDocument.Parse(line[6..]);
                            failure = document.RootElement.GetProperty("message").GetString();
                        }
                        catch (JsonException) { }
                        catch (KeyNotFoundException) { }
                        catch (InvalidOperationException) { }
                    }
                    else if (failure is null && line.StartsWith("Error:", StringComparison.Ordinal)) failure = line[6..].Trim();
                    if (line.Contains("falling back", StringComparison.OrdinalIgnoreCase))
                        progress.Report("GPU unavailable; continuing with CPU. " + line);
                }
            }
            var output = ReadOutput(); var error = ReadErrors();
            await process.WaitForExitAsync(CancellationToken.None);
            await Task.WhenAll(output, error);
            token.ThrowIfCancellationRequested();
            await diagnostics.FlushAsync();
            if (process.ExitCode != 0 || result is null)
                throw new AlignmentProcessingException($"Alignment failed: {failure ?? $"The processing worker exited with code {process.ExitCode}."}{Environment.NewLine}Details: {logFile}", logFile);
            var absolute = Path.GetFullPath(result);
            var prefix = Path.GetFullPath(cacheFolder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!absolute.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || !File.Exists(absolute))
                throw new InvalidDataException("Engine returned an unexpected result path.");
            return absolute;
        }
        finally { if (File.Exists(jobFile)) File.Delete(jobFile); }
    }
}
