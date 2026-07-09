using System;
using System.Collections.Generic;

namespace Audibly.App.Services.Transcription;

/// <summary>
///     A file expected inside an installed speech model. MinBytes is a sanity floor,
///     not an exact size (release assets may be rebuilt upstream).
/// </summary>
public record SpeechModelFile(string Name, long MinBytes);

/// <summary>
///     Describes a downloadable speech-to-text model. The transcription pipeline and the
///     model download service only ever see this descriptor, never backend specifics, so
///     models/backends can be swapped without touching the application layer.
/// </summary>
public record SpeechModelDescriptor(
    string Id,
    string DisplayName,
    string DownloadUrl,
    IReadOnlyList<SpeechModelFile> ExpectedFiles,
    long DownloadSizeBytes,
    long DiskSizeBytes,
    int RequiredSampleRate,
    string LicenseName,
    string LicenseUrl,
    string AttributionText)
{
    public string DownloadSizeText => $"{DownloadSizeBytes / 1024.0 / 1024.0:F0} MB";
    public string DiskSizeText => $"{DiskSizeBytes / 1024.0 / 1024.0:F0} MB";
}

public static class SpeechModels
{
    /// <summary>
    ///     NVIDIA Parakeet TDT 0.6B v3 (multilingual, punctuation + capitalization), int8,
    ///     packaged by the sherpa-onnx project.
    /// </summary>
    public static readonly SpeechModelDescriptor ParakeetTdtV3Int8 = new(
        "sherpa-onnx-nemo-parakeet-tdt-0.6b-v3-int8",
        "NVIDIA Parakeet TDT 0.6B v3 (multilingual)",
        "https://github.com/k2-fsa/sherpa-onnx/releases/download/asr-models/sherpa-onnx-nemo-parakeet-tdt-0.6b-v3-int8.tar.bz2",
        new SpeechModelFile[]
        {
            new("encoder.int8.onnx", 400_000_000),
            new("decoder.int8.onnx", 4_000_000),
            new("joiner.int8.onnx", 2_000_000),
            new("tokens.txt", 10_000)
        },
        487_170_055,
        671_000_000,
        16000,
        "CC-BY-4.0",
        "https://huggingface.co/nvidia/parakeet-tdt-0.6b-v3",
        "NVIDIA Parakeet TDT 0.6B v3, converted by the sherpa-onnx project");
}
