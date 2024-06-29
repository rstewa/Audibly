// Author: rstewa · https://github.com/rstewa
// Created: 6/13/2024
// Updated: 6/13/2024

using Windows.Storage;

namespace Audibly.App.Helpers;

public static class UserSettings
{
    public static double Volume
    {
        get
        {
            var volume = ApplicationData.Current.LocalSettings.Values["Volume"];
            if (volume != null) return (double)volume;
            
            ApplicationData.Current.LocalSettings.Values["Volume"] = 100;
            return 100;

        }
        set => ApplicationData.Current.LocalSettings.Values["Volume"] = value;
    }
    
    public static double PlaybackSpeed
    {
        get
        {
            var playbackSpeed = ApplicationData.Current.LocalSettings.Values["PlaybackSpeed"];
            if (playbackSpeed != null) return (double)playbackSpeed;
            
            ApplicationData.Current.LocalSettings.Values["PlaybackSpeed"] = 1;
            return 1;
        }
        set => ApplicationData.Current.LocalSettings.Values["PlaybackSpeed"] = value;
    }
}