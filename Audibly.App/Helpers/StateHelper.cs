// Author: rstewa · https://github.com/rstewa
// Created: 3/22/2024
// Updated: 3/22/2024

using Windows.Storage;

namespace Audibly.App.Helpers;

public static class StateHelper
{
    public static string GetThemeSetting()
    {
        return ApplicationData.Current.LocalSettings.Values["themeSetting"].ToString();
    }
    
    public static void SetThemeSetting(string theme)
    {
        ApplicationData.Current.LocalSettings.Values["themeSetting"] = theme;
    }
    
    public static void SetLastPlayedAudiobook(string audiobookId)
    {
        ApplicationData.Current.LocalSettings.Values["lastPlayedAudiobook"] = audiobookId;
    }
    
    public static string GetLastPlayedAudiobook()
    {
        return ApplicationData.Current.LocalSettings.Values["lastPlayedAudiobook"].ToString();
    }
}