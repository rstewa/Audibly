//   Author: Ryan Stewart
//   Date: 07/25/2023

using Audibly.Extensions;
using System;
using Windows.Storage;

namespace Audibly.Helpers;

public static class AudiblySettingsHelper
{
    private static readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

    public static string CurrentAudiobookPath
    {
        get => _localSettings.Values["currentAudiobookPath"]?.ToString();
        set => _localSettings.Values["currentAudiobookPath"] = value;
    }

    public static string CurrentBookName { get; set; }

    private static string CurrentPositionLabel => $"{CurrentBookName ?? string.Empty}:CurrentPosition";
    public static double? CurrentPosition
    {
        get => _localSettings.Values[CurrentPositionLabel].ToDouble();
        set => _localSettings.Values[CurrentPositionLabel] = value;
    }

    private static string VolumeLabel => $"{CurrentBookName}:Volume";
    public static double? Volume
    {
        get => _localSettings.Values[VolumeLabel]?.ToDouble();
        set => _localSettings.Values[VolumeLabel] = value?.ToString();
    }

    private static string TimePlayerPausedLabel => $"{CurrentBookName}:TimePlayerWasPaused";
    public static DateTime? TimePlayerPaused
    {
        get => _localSettings.Values[TimePlayerPausedLabel] as DateTime?;
        set => _localSettings.Values[TimePlayerPausedLabel] = value;
    }
}
