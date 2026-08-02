// Author: rstewa · https://github.com/rstewa
// Updated: 05/08/2025

using System;
using System.Text.Json;
using Windows.Storage;
using Sentry;

namespace Audibly.App.Helpers;

/// <summary>
///     Controls what the reverse/forward buttons in the Windows media overlay do.
/// </summary>
public enum MediaControlBehavior
{
    /// <summary>Reverse/forward skip back/forward by the configured number of seconds.</summary>
    SkipBySeconds,

    /// <summary>Reverse/forward go to the previous/next chapter.</summary>
    ChapterNavigation
}

/// <summary>
///     Controls what is shown as the primary title in the Windows media overlay.
/// </summary>
public enum MediaTitleDisplayKind
{
    /// <summary>Shows the book title as the primary title.</summary>
    BookTitle,

    /// <summary>Shows the current chapter name as the primary title.</summary>
    ChapterTitle
}

public static class UserSettings
{
    /// <summary>
    ///     Raised when the skip back/forward amounts change, so UI can refresh its labels/tooltips.
    /// </summary>
    public static event Action? SkipAmountsChanged;

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

    public static double SkipBackSeconds
    {
        get
        {
            try
            {
                var skipBackSeconds = ApplicationData.Current.LocalSettings.Values["SkipBackSeconds"];
                if (skipBackSeconds != null)
                    if (double.TryParse(skipBackSeconds.ToString(), out var result))
                        return result;

                ApplicationData.Current.LocalSettings.Values["SkipBackSeconds"] = 10;
                return 10;
            }
            catch (Exception e)
            {
                // log to sentry
                SentrySdk.CaptureException(e);
                return 10;
            }
        }
        set
        {
            ApplicationData.Current.LocalSettings.Values["SkipBackSeconds"] = value;
            SkipAmountsChanged?.Invoke();
        }
    }

    public static double SkipForwardSeconds
    {
        get
        {
            try
            {
                var skipForwardSeconds = ApplicationData.Current.LocalSettings.Values["SkipForwardSeconds"];
                if (skipForwardSeconds != null)
                    if (double.TryParse(skipForwardSeconds.ToString(), out var result))
                        return result;

                ApplicationData.Current.LocalSettings.Values["SkipForwardSeconds"] = 30;
                return 30;
            }
            catch (Exception e)
            {
                // log to sentry
                SentrySdk.CaptureException(e);
                return 30;
            }
        }
        set
        {
            ApplicationData.Current.LocalSettings.Values["SkipForwardSeconds"] = value;
            SkipAmountsChanged?.Invoke();
        }
    }

    public static MediaControlBehavior MediaButtonsBehavior
    {
        get
        {
            try
            {
                var mediaButtonsBehavior =
                    ApplicationData.Current.LocalSettings.Values["MediaButtonsBehavior"]?.ToString();
                if (mediaButtonsBehavior != null &&
                    Enum.TryParse(mediaButtonsBehavior, out MediaControlBehavior result))
                    return result;

                ApplicationData.Current.LocalSettings.Values["MediaButtonsBehavior"] =
                    MediaControlBehavior.SkipBySeconds.ToString();
                return MediaControlBehavior.SkipBySeconds;
            }
            catch (Exception e)
            {
                // log to sentry
                SentrySdk.CaptureException(e);
                return MediaControlBehavior.SkipBySeconds;
            }
        }
        set => ApplicationData.Current.LocalSettings.Values["MediaButtonsBehavior"] = value.ToString();
    }

    public static MediaTitleDisplayKind MediaTitleDisplayMode
    {
        get
        {
            try
            {
                var mediaTitleDisplay =
                    ApplicationData.Current.LocalSettings.Values["MediaTitleDisplayMode"]?.ToString();
                if (mediaTitleDisplay != null && Enum.TryParse(mediaTitleDisplay, out MediaTitleDisplayKind result))
                    return result;

                ApplicationData.Current.LocalSettings.Values["MediaTitleDisplayMode"] =
                    MediaTitleDisplayKind.BookTitle.ToString();
                return MediaTitleDisplayKind.BookTitle;
            }
            catch (Exception e)
            {
                // log to sentry
                SentrySdk.CaptureException(e);
                return MediaTitleDisplayKind.BookTitle;
            }
        }
        set => ApplicationData.Current.LocalSettings.Values["MediaTitleDisplayMode"] = value.ToString();
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

    /// <summary>
    ///     Gets or sets the list of FutureAccessList tokens for watched folders.
    /// </summary>
    public static string[] WatchedFolderTokens
    {
        get
        {
            try
            {
                var json = ApplicationData.Current.LocalSettings.Values["WatchedFolderTokens"]?.ToString();
                if (!string.IsNullOrEmpty(json))
                {
                    var result = JsonSerializer.Deserialize<string[]>(json);
                    if (result != null) return result;
                }

                return [];
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
                return [];
            }
        }
        set => ApplicationData.Current.LocalSettings.Values["WatchedFolderTokens"] =
            JsonSerializer.Serialize(value);
    }
}