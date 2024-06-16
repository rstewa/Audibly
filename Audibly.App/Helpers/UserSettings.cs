// Author: rstewa · https://github.com/rstewa
// Created: 6/13/2024
// Updated: 6/13/2024

using Windows.Storage;

namespace Audibly.App.Helpers;

public static class UserSettings
{
    public static string Volume
    {
        get
        {
            var volume = ApplicationData.Current.LocalSettings.Values["Volume"] as string;
            return string.IsNullOrEmpty(volume) ? "100" : volume;
        }
        set => ApplicationData.Current.LocalSettings.Values["Volume"] = value;
    }
    
    public static string PlaybackSpeed
    {
        get
        {
            var playbackSpeed = ApplicationData.Current.LocalSettings.Values["PlaybackSpeed"] as string;
            return string.IsNullOrEmpty(playbackSpeed) ? "1" : playbackSpeed;
        }
        set => ApplicationData.Current.LocalSettings.Values["PlaybackSpeed"] = value;
    }
}