using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using SharpCompress.Readers;

namespace Audibly.App.Services.Transcription;

public enum SpeechModelState
{
    NotDownloaded,
    Downloading,
    Extracting,
    Ready,
    Failed
}

/// <summary>
///     Downloads, verifies and deletes the active speech model. Entirely descriptor-driven:
///     the model lives in LocalFolder\Models\{descriptor.Id}\ with a model.json manifest
///     recording the verified file sizes; the archive download resumes from a .partial file.
/// </summary>
public class TranscriptionModelService
{
    private const string ManifestFileName = "model.json";

    private static readonly HttpClient HttpClient = new() { Timeout = Timeout.InfiniteTimeSpan };

    private readonly SemaphoreSlim _gate = new(1, 1);

    public TranscriptionModelService(SpeechModelDescriptor descriptor)
    {
        Descriptor = descriptor;
        ModelDirectory = Path.Combine(ApplicationData.Current.LocalFolder.Path, "Models", descriptor.Id);
        ArchivePath = ModelDirectory + ".tar.bz2";
    }

    public SpeechModelDescriptor Descriptor { get; }

    public string ModelDirectory { get; }

    private string ArchivePath { get; }

    private string PartialArchivePath => ArchivePath + ".partial";

    public SpeechModelState State { get; private set; } = SpeechModelState.NotDownloaded;

    /// <summary>
    ///     Download progress in [0, 1]; only meaningful while Downloading.
    /// </summary>
    public double DownloadProgress { get; private set; }

    public string? LastError { get; private set; }

    /// <summary>
    ///     Raised on any state or progress change, on an arbitrary thread.
    /// </summary>
    public event Action? StateChanged;

    /// <summary>
    ///     Checks the manifest and files on disk; sets State to Ready or NotDownloaded.
    /// </summary>
    public async Task<bool> VerifyInstalledAsync()
    {
        await _gate.WaitAsync();
        try
        {
            SetState(IsInstallValid() ? SpeechModelState.Ready : SpeechModelState.NotDownloaded);
            return State == SpeechModelState.Ready;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    ///     Full install flow: download (resuming any .partial), extract, verify, write manifest.
    ///     Cancellation keeps the partial download for a later resume.
    /// </summary>
    public async Task DownloadAndInstallAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(CancellationToken.None);
        try
        {
            if (IsInstallValid())
            {
                SetState(SpeechModelState.Ready);
                return;
            }

            LastError = null;
            await DownloadArchiveAsync(cancellationToken);

            SetState(SpeechModelState.Extracting);
            await Task.Run(() => ExtractArchive(cancellationToken), cancellationToken);

            WriteManifest();
            if (!IsInstallValid())
                throw new InvalidOperationException("Model files failed verification after extraction.");

            File.Delete(ArchivePath);
            SetState(SpeechModelState.Ready);
        }
        catch (OperationCanceledException)
        {
            // keep the .partial file so the next attempt resumes
            SetState(SpeechModelState.NotDownloaded);
        }
        catch (Exception e)
        {
            App.ViewModel?.LoggingService.LogError(e, true);
            LastError = e.Message;
            SetState(SpeechModelState.Failed);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    ///     Removes the installed model (and any leftover archives).
    /// </summary>
    public async Task DeleteAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (Directory.Exists(ModelDirectory)) Directory.Delete(ModelDirectory, true);
            if (File.Exists(ArchivePath)) File.Delete(ArchivePath);
            if (File.Exists(PartialArchivePath)) File.Delete(PartialArchivePath);
            LastError = null;
            SetState(SpeechModelState.NotDownloaded);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task DownloadArchiveAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(ArchivePath)) return; // fully downloaded earlier, extraction was interrupted

        Directory.CreateDirectory(Path.GetDirectoryName(ArchivePath)!);

        var existingBytes = File.Exists(PartialArchivePath) ? new FileInfo(PartialArchivePath).Length : 0L;
        if (existingBytes > 0 && existingBytes >= Descriptor.DownloadSizeBytes)
        {
            // a previous run finished the download but was killed before the rename
            File.Move(PartialArchivePath, ArchivePath, true);
            return;
        }

        var drive = new DriveInfo(Path.GetPathRoot(ModelDirectory)!);
        var required = Descriptor.DownloadSizeBytes - existingBytes + Descriptor.DiskSizeBytes + 100_000_000;
        if (drive.AvailableFreeSpace < required)
            throw new IOException(
                $"Not enough free disk space: need about {required / 1024 / 1024} MB on {drive.Name}.");

        DownloadProgress = 0;
        SetState(SpeechModelState.Downloading);

        using var request = new HttpRequestMessage(HttpMethod.Get, Descriptor.DownloadUrl);
        if (existingBytes > 0)
            request.Headers.Range = new RangeHeaderValue(existingBytes, null);

        using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var resuming = response.StatusCode == System.Net.HttpStatusCode.PartialContent && existingBytes > 0;
        if (!resuming) existingBytes = 0;

        var totalBytes = existingBytes + (response.Content.Headers.ContentLength ?? Descriptor.DownloadSizeBytes);

        await using (var target = new FileStream(PartialArchivePath,
                         resuming ? FileMode.Append : FileMode.Create, FileAccess.Write))
        await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken))
        {
            var buffer = new byte[81920];
            var written = existingBytes;
            var lastReported = written;
            int read;
            while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                written += read;
                if (written - lastReported >= 4 * 1024 * 1024)
                {
                    lastReported = written;
                    DownloadProgress = Math.Clamp((double)written / totalBytes, 0, 1);
                    StateChanged?.Invoke();
                }
            }
        }

        var downloaded = new FileInfo(PartialArchivePath).Length;
        if (downloaded < totalBytes)
            throw new IOException($"Download ended early ({downloaded} of {totalBytes} bytes).");

        File.Move(PartialArchivePath, ArchivePath, true);
        DownloadProgress = 1;
        StateChanged?.Invoke();
    }

    private void ExtractArchive(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(ModelDirectory);

        using var archiveStream = File.OpenRead(ArchivePath);
        using var reader = ReaderFactory.Open(archiveStream, new ReaderOptions());
        var buffer = new byte[81920];

        while (reader.MoveToNextEntry())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (reader.Entry.IsDirectory) continue;

            var relativePath = NormalizeEntryPath(reader.Entry.Key);
            if (relativePath == null) continue;
            // the archive's sample audio is not needed by the app
            if (relativePath.StartsWith("test_wavs/", StringComparison.OrdinalIgnoreCase)) continue;

            var destination = Path.Combine(ModelDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

            using var entryStream = reader.OpenEntryStream();
            using var outputStream = File.Create(destination);
            int read;
            while ((read = entryStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                outputStream.Write(buffer, 0, read);
            }
        }
    }

    /// <summary>
    ///     Strips the archive's single root directory (e.g. "model-name/tokens.txt" -> "tokens.txt").
    /// </summary>
    private static string? NormalizeEntryPath(string? entryKey)
    {
        if (string.IsNullOrEmpty(entryKey)) return null;

        var normalized = entryKey.Replace('\\', '/').TrimStart('.', '/');
        var separatorIndex = normalized.IndexOf('/');
        if (separatorIndex < 0) return normalized;

        var stripped = normalized[(separatorIndex + 1)..];
        return string.IsNullOrEmpty(stripped) ? null : stripped;
    }

    private void WriteManifest()
    {
        var files = Descriptor.ExpectedFiles
            .Select(f => new ManifestFile(f.Name, new FileInfo(Path.Combine(ModelDirectory, f.Name)).Length))
            .ToArray();
        var manifest = new Manifest(Descriptor.Id, files, DateTime.UtcNow);
        File.WriteAllText(Path.Combine(ModelDirectory, ManifestFileName),
            JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
    }

    private bool IsInstallValid()
    {
        try
        {
            var manifestPath = Path.Combine(ModelDirectory, ManifestFileName);
            if (!File.Exists(manifestPath)) return false;

            var manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(manifestPath));
            if (manifest == null || manifest.ModelId != Descriptor.Id) return false;

            foreach (var expected in Descriptor.ExpectedFiles)
            {
                var path = Path.Combine(ModelDirectory, expected.Name);
                if (!File.Exists(path)) return false;

                var length = new FileInfo(path).Length;
                if (length < expected.MinBytes) return false;

                var recorded = manifest.Files.FirstOrDefault(f => f.Name == expected.Name);
                if (recorded == null || recorded.Size != length) return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private void SetState(SpeechModelState state)
    {
        State = state;
        StateChanged?.Invoke();
    }

    private record Manifest(string ModelId, ManifestFile[] Files, DateTime DownloadedAtUtc);

    private record ManifestFile(string Name, long Size);
}
