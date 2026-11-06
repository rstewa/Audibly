using System.Diagnostics;
using System.Text.Json;

namespace Audibly.ReadAlong;

public sealed class StorytellerProcessor
{
    public async Task<string> GenerateAsync(string engineFolder, string cacheFolder, Guid bookId,
        string epub, IReadOnlyList<string> sources, string language, IProgress<string> progress, CancellationToken token)
    {
        if (!File.Exists(Path.Combine(engineFolder, "align.mjs")))
            throw new FileNotFoundException("Read Along components are missing from this installation. Repair or reinstall Audibly.");
        Directory.CreateDirectory(cacheFolder);
        var jobFile = Path.Combine(cacheFolder, Guid.NewGuid() + ".job.json");
        try
        {
            await File.WriteAllTextAsync(jobFile, JsonSerializer.Serialize(new
            { audiobookId = bookId, epub, sources, language, cache = cacheFolder }, AlignmentMap.Json), token);
            var localNode = Path.Combine(engineFolder, "node_modules", "node", "bin", "node.exe");
            var start = new ProcessStartInfo(File.Exists(localNode) ? localNode : "node.exe")
            {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true,
                RedirectStandardError = true, WorkingDirectory = engineFolder
            };
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
            var errors = new Queue<string>();
            async Task ReadOutput()
            {
                while (await process.StandardOutput.ReadLineAsync() is { } line)
                    if (line.StartsWith("RESULT:", StringComparison.Ordinal)) result = line[7..];
                    else progress.Report(line.Length > 240 ? line[..240] : line);
            }
            async Task ReadErrors()
            {
                while (await process.StandardError.ReadLineAsync() is { } line)
                {
                    if (errors.Count == 12) errors.Dequeue();
                    errors.Enqueue(line.Length > 500 ? line[..500] : line);
                }
            }
            var output = ReadOutput(); var error = ReadErrors();
            await process.WaitForExitAsync(CancellationToken.None);
            await Task.WhenAll(output, error);
            token.ThrowIfCancellationRequested();
            if (process.ExitCode != 0 || result is null) throw new IOException("Alignment failed: " + string.Join(Environment.NewLine, errors));
            var absolute = Path.GetFullPath(result);
            var prefix = Path.GetFullPath(cacheFolder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!absolute.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || !File.Exists(absolute))
                throw new InvalidDataException("Engine returned an unexpected result path.");
            return absolute;
        }
        finally { if (File.Exists(jobFile)) File.Delete(jobFile); }
    }
}
