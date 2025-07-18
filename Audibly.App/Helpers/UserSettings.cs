// Author: rstewa · https://github.com/rstewa
// Updated: 05/08/2025

using System;
using Windows.Storage;
using Sentry;

namespace Audibly.App.Helpers;

public static class UserSettings
{
    public static bool ShowDataMigrationFailedDialog
    {
        get
        {
            try
            {
                var dataMigrationFailed = ApplicationData.Current.LocalSettings.Values["DataMigrationFailed"];
                if (dataMigrationFailed != null)
                    if (bool.TryParse(dataMigrationFailed.ToString(), out var result))
                        return result;

                ApplicationData.Current.LocalSettings.Values["DataMigrationFailed"] = false;
                return false;
            }
            catch (Exception e)
            {
                // log to sentry
                SentrySdk.CaptureException(e);
                return false;
            }
        }
        set => ApplicationData.Current.LocalSettings.Values["DataMigrationFailed"] = value;
    }

    public static bool NeedToImportAudiblyExport
    {
        get
        {
            try
            {
                var needToImport = ApplicationData.Current.LocalSettings.Values["NeedToImportAudiblyExportFile"];
                if (needToImport != null)
                    if (bool.TryParse(needToImport.ToString(), out var result))
                        return result;

                ApplicationData.Current.LocalSettings.Values["NeedToImportAudiblyExportFile"] = false;
                return false;
            }
            catch (Exception e)
            {
                // log to sentry
                SentrySdk.CaptureException(e);
                return false;
            }
        }
        set => ApplicationData.Current.LocalSettings.Values["NeedToImportAudiblyExportFile"] = value;
    }

    public static string? Version
    {
        get
        {
            var version = ApplicationData.Current.LocalSettings.Values["CurrentVersion"];
            return version?.ToString();
        }
        set => ApplicationData.Current.LocalSettings.Values["CurrentVersion"] = value;
    }

    public static string? PreviousVersion
    {
        get
        {
            var version = ApplicationData.Current.LocalSettings.Values["PreviousVersion"];
            return version?.ToString();
        }
        set => ApplicationData.Current.LocalSettings.Values["PreviousVersion"] = value;
    }

    public static double ZoomLevel
    {
        get
        {
            try
            {
                var zoomLevel = ApplicationData.Current.LocalSettings.Values["ZoomLevel"];
                if (zoomLevel != null)
                    if (double.TryParse(zoomLevel.ToString(), out var result))
                        return result;

                ApplicationData.Current.LocalSettings.Values["ZoomLevel"] = 100;
                return 100;
            }
            catch (Exception e)
            {
                // log to sentry
                SentrySdk.CaptureException(e);
                return 100;
            }
        }
        set => ApplicationData.Current.LocalSettings.Values["ZoomLevel"] = value;
    }

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