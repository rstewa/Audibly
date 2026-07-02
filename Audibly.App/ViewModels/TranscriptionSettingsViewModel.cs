using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Audibly.App.Helpers;
using Audibly.App.Services.Transcription;
using Microsoft.UI.Dispatching;

namespace Audibly.App.ViewModels;

/// <summary>
///     Backs the "AI Read-Along" section of the settings page and the transcript pane's
///     model/enablement placeholders.
/// </summary>
public class TranscriptionSettingsViewModel : BindableBase
{
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private readonly TranscriptionModelService _modelService;

    private CancellationTokenSource? _downloadCts;

    public TranscriptionSettingsViewModel(TranscriptionModelService modelService)
    {
        _modelService = modelService;
        _modelService.StateChanged += () => _dispatcherQueue.TryEnqueue(NotifyModelStateChanged);
        if (App.Transcription != null)
            App.Transcription.ActivityChanged +=
                () => _dispatcherQueue.TryEnqueue(() => OnPropertyChanged(nameof(ActivityText)));
    }

    public string ActivityText => App.Transcription?.ActivityDescription ?? "Idle";

    public SpeechModelDescriptor Descriptor => _modelService.Descriptor;

    public Uri ModelLicenseUri => new(Descriptor.LicenseUrl);

    public string ModelAttributionLabel => $"Model: {Descriptor.AttributionText} ({Descriptor.LicenseName})";

    /// <summary>
    ///     Transcription needs a 64-bit process: the int8 encoder alone is ~650 MB,
    ///     which a 32-bit address space cannot reliably map.
    /// </summary>
    public bool IsSupported => RuntimeInformation.ProcessArchitecture != Architecture.X86;

    public string SectionDescription => IsSupported
        ? "Transcribe audiobooks on this device with a local AI speech model to enable a synced read-along view. Nothing ever leaves your device."
        : "AI transcription requires a 64-bit (x64 or ARM64) device.";

    public bool IsEnabled
    {
        get => UserSettings.TranscriptionEnabled && IsSupported;
        set
        {
            UserSettings.TranscriptionEnabled = value;
            OnPropertyChanged();
            App.Transcription?.OnSettingsChanged();
        }
    }

    /// <summary>
    ///     0 = Off (manual only), 1 = currently playing book, 2 = entire library.
    /// </summary>
    public int Scope
    {
        get => UserSettings.TranscriptionScope;
        set
        {
            UserSettings.TranscriptionScope = value;
            OnPropertyChanged();
            App.Transcription?.OnSettingsChanged();
        }
    }

    /// <summary>
    ///     Decode threads; 0 = automatic (last-level-cache topology minus one).
    /// </summary>
    public double TranscriptionThreads
    {
        get => UserSettings.TranscriptionThreads;
        set
        {
            UserSettings.TranscriptionThreads = (int)value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ThreadCountDescription));
        }
    }

    public double MaxThreads => Environment.ProcessorCount;

    public string ThreadCountDescription =>
        $"0 = automatic ({SherpaOnnxParakeetBackend.ResolveThreadCount()} on this device). " +
        "Takes effect the next time the speech model loads.";

    public SpeechModelState ModelState => _modelService.State;

    public bool IsModelReady => _modelService.State == SpeechModelState.Ready;

    public bool IsDownloading => _modelService.State == SpeechModelState.Downloading;

    public bool IsExtracting => _modelService.State == SpeechModelState.Extracting;

    public bool IsBusy => IsDownloading || IsExtracting;

    public bool CanDownload => IsSupported &&
                               _modelService.State is SpeechModelState.NotDownloaded or SpeechModelState.Failed;

    public double DownloadProgressPercent => _modelService.DownloadProgress * 100;

    public string ModelStatusText => _modelService.State switch
    {
        SpeechModelState.NotDownloaded =>
            $"Not downloaded — {Descriptor.DownloadSizeText} download, {Descriptor.DiskSizeText} on disk",
        SpeechModelState.Downloading => $"Downloading… {DownloadProgressPercent:F0}%",
        SpeechModelState.Extracting => "Extracting…",
        SpeechModelState.Ready => $"Installed ({Descriptor.DiskSizeText})",
        SpeechModelState.Failed => $"Failed: {_modelService.LastError}",
        _ => ""
    };

    public async Task DownloadModelAsync()
    {
        if (!CanDownload) return;

        _downloadCts = new CancellationTokenSource();
        try
        {
            await _modelService.DownloadAndInstallAsync(_downloadCts.Token);
        }
        finally
        {
            _downloadCts.Dispose();
            _downloadCts = null;
        }
    }

    public void CancelModelDownload()
    {
        _downloadCts?.Cancel();
    }

    public async Task DeleteModelAsync()
    {
        if (App.Transcription != null) await App.Transcription.StopAndUnloadAsync();
        await _modelService.DeleteAsync();
    }

    private void NotifyModelStateChanged()
    {
        OnPropertyChanged(nameof(ModelState));
        OnPropertyChanged(nameof(IsModelReady));
        OnPropertyChanged(nameof(IsDownloading));
        OnPropertyChanged(nameof(IsExtracting));
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(CanDownload));
        OnPropertyChanged(nameof(DownloadProgressPercent));
        OnPropertyChanged(nameof(ModelStatusText));
    }
}
