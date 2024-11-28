// Author: rstewa · https://github.com/rstewa
// Created: 06/16/2024
// Updated: 07/01/2024

using System;
using Windows.Storage;
using Sentry;

namespace Audibly.App.Helpers;

public static class UserSettings
{
    public static double Volume
    {
        get
        {
            try
            {
                var volume = ApplicationData.Current.LocalSettings.Values["Volume"];
                if (volume != null)
                    if (double.TryParse(volume.ToString(), out var result))
                        return result;

                ApplicationData.Current.LocalSettings.Values["Volume"] = 100;
                return 100;
            }
            catch (Exception e)
            {
                // log to sentry
                SentrySdk.CaptureException(e);
                return 100;
            }
        }
        set => ApplicationData.Current.LocalSettings.Values["Volume"] = value;
    }

    public static double PlaybackSpeed
    {
        get
        {
            try
            {
                var playbackSpeed = ApplicationData.Current.LocalSettings.Values["PlaybackSpeed"];
                if (playbackSpeed != null)
                    if (double.TryParse(playbackSpeed.ToString(), out var result))
                        return result;

                ApplicationData.Current.LocalSettings.Values["PlaybackSpeed"] = 1;
                return 1;
            }
            catch (Exception e)
            {
                // log to sentry
                SentrySdk.CaptureException(e);
                return 1;
            }
        }
        set => ApplicationData.Current.LocalSettings.Values["PlaybackSpeed"] = value;
    }
    
    public static bool IsSidebarCollapsed
    {
        get
        {
            try
            {
                var isCollapsed = ApplicationData.Current.LocalSettings.Values["IsSidebarCollapsed"];
                if (isCollapsed != null)
                    if (bool.TryParse(isCollapsed.ToString(), out var result))
                        return result;

                ApplicationData.Current.LocalSettings.Values["IsSidebarCollapsed"] = false;
                return false;
            }
            catch (Exception e)
            {
                // log to sentry
                SentrySdk.CaptureException(e);
                return false;
            }
        }
        set => ApplicationData.Current.LocalSettings.Values["IsSidebarCollapsed"] = value;
    }
}