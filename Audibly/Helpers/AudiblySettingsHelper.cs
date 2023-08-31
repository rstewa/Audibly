//   Author: Ryan Stewart
//   Date: 07/25/2023

using Audibly.Extensions;
using System;
using Windows.Storage;

namespace Audibly.Helpers;

public static class AudiblySettingsHelper
{
    private static readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

    public static void ClearSettings()
    {
        _localSettings.Values.Clear();
    }
    
    public static string CurrentBookName { get; set; }

    public static string CurrentAudiobookPath
    {
        get => _localSettings.Values[nameof(CurrentAudiobookPath)]?.ToString();
        set => _localSettings.Values[nameof(CurrentAudiobookPath)] = value;
    }

    private static string CurrentPositionLabel => $"{CurrentBookName}:{nameof(CurrentPosition)}";
    public static double? CurrentPosition
    {
        get => _localSettings.Values[CurrentPositionLabel].ToDouble();
        set => _localSettings.Values[CurrentPositionLabel] = value;
    }

    private static string VolumeLabel => $"{CurrentBookName}:{nameof(Volume)}";
    public static double? Volume
    {
        get => _localSettings.Values[VolumeLabel]?.ToDouble();
        set => _localSettings.Values[VolumeLabel] = value?.ToString();
    }

    private static string TimePlayerPausedLabel => $"{CurrentBookName}:{nameof(TimePlayerPaused)}";
    public static DateTime? TimePlayerPaused
    {
        get => _localSettings.Values[TimePlayerPausedLabel] as DateTime?;
        set => _localSettings.Values[TimePlayerPausedLabel] = value;
    }

    private static string PlaybackSpeedLabel => $"{CurrentBookName}:{nameof(PlaybackSpeed)}";
    public static double? PlaybackSpeed
    {
        get => _localSettings.Values[PlaybackSpeedLabel].ToDouble();
        set => _localSettings.Values[PlaybackSpeedLabel] = value;
    }

}
