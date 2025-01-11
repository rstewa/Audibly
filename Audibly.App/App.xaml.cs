// Author: rstewa · https://github.com/rstewa
// Created: 04/15/2024
// Updated: 10/03/2024

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Audibly.App.Extensions;
using Audibly.App.Helpers;
using Audibly.App.Services;
using Audibly.App.ViewModels;
using Audibly.Models;
using Audibly.Models.v1;
using Audibly.Repository.Interfaces;
using Audibly.Repository.Sql;
using CommunityToolkit.WinUI;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.AppLifecycle;
using Sentry;
using WinRT.Interop;
using Constants = Audibly.App.Helpers.Constants;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;

namespace Audibly.App;

/// <summary>
///     Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private static Win32WindowHelper win32WindowHelper;
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    /// <summary>
    ///     Initializes the singleton application object.  This is the first line of authored code
    ///     executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        SentrySdk.Init(options =>
        {
            options.Dsn = Helpers.Sentry.Dsn;
            options.AutoSessionTracking = true;
            options.SampleRate = 0.25f;
            options.TracesSampleRate = 0.25;
            options.IsGlobalModeEnabled = true;
            options.ProfilesSampleRate = 0.25;
            options.Environment = "production";
        });

        InitializeComponent();
        UnhandledException += OnUnhandledException;
    }

    /// <summary>
    ///     Gets main App Window
    /// </summary>
    public static Window Window { get; private set; }

    /// <summary>
    ///     Gets the app-wide MainViewModel singleton instance.
    /// </summary>
    public static MainViewModel ViewModel { get; } =
        new(new FileImportService(), new AppDataService(),
            new LoggingService(ApplicationData.Current.LocalFolder.Path + @"\Audibly.log"));

    /// <summary>
    ///     Gets the app-wide PlayerViewModel singleton instance.
    /// </summary>
    public static PlayerViewModel PlayerViewModel { get; } = new();

    /// <summary>
    ///     Pipeline for interacting with backend service or database.
    /// </summary>
    public static IAudiblyRepository Repository { get; private set; }

    /// <summary>
    ///     Gets the root frame of the app. This contains the nav view and the player page
    /// </summary>
    public static Frame? RootFrame { get; private set; }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        ViewModel.LoggingService.LogError(e.Exception, true);
        // RestartApp();
    }

    /// <summary>
    ///     Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // If this is the first instance launched, then register it as the "main" instance.
        // If this isn't the first instance launched, then "main" will already be registered,
        // so retrieve it.
        var mainInstance = AppInstance.FindOrRegisterForKey("main");
        mainInstance.Activated += OnAppInstanceActivated;

        // If the instance that's executing the OnLaunched handler right now
        // isn't the "main" instance.
        if (!mainInstance.IsCurrent)
        {
            // Redirect the activation (and args) to the "main" instance, and exit.
            var activatedEventArgs =
                AppInstance.GetCurrent().GetActivatedEventArgs();
            await mainInstance.RedirectActivationToAsync(activatedEventArgs);
            Process.GetCurrentProcess().Kill();
            return;
        }

        Window = WindowHelper.CreateWindow();

        var appWindow = WindowHelper.GetAppWindow(Window);
        appWindow.Closing += async (_, _) =>
        {
            if (PlayerViewModel.NowPlaying != null) await PlayerViewModel.NowPlaying.SaveAsync();
        };

        win32WindowHelper = new Win32WindowHelper(Window);
        // todo: uncomment this
        win32WindowHelper.SetWindowMinMaxSize(new Win32WindowHelper.POINT { x = 640, y = 640 });

        UseSqlite();

        RootFrame = Window.Content as Frame;

        if (RootFrame == null)
        {
            RootFrame = new Frame();
            RootFrame.NavigationFailed += OnNavigationFailed;
            Window.Content = RootFrame;
        }

        if (RootFrame.Content == null) RootFrame.Navigate(typeof(AppShell), args.Arguments);

        Window.CustomizeWindow(-1, -1, true, true, true, true, true, true);

        ThemeHelper.Initialize();

        // handle file activation
        // got this from Andrew KeepCoding's answer here: https://stackoverflow.com/questions/76650127/how-to-handle-activation-through-files-in-winui-3-packaged
        var appActivationArguments = AppInstance.GetCurrent().GetActivatedEventArgs();
        if (appActivationArguments.Kind is ExtendedActivationKind.File &&
            appActivationArguments.Data is IFileActivatedEventArgs fileActivatedEventArgs &&
            fileActivatedEventArgs.Files.FirstOrDefault() is IStorageFile storageFile)
            await _dispatcherQueue.EnqueueAsync(() => HandleFileActivation(storageFile));

        Window.Activate();
    }

    private async void HandleFileActivation(IStorageFile storageFile, bool onAppInstanceActivated = false)
    {
        ViewModel.LoggingService.Log($"File activated: {storageFile.Path}");

        try
        {
            if (onAppInstanceActivated)
            {
                // if the app is already running and a file is opened, then we need to handle it differently
                await _dispatcherQueue.EnqueueAsync(() => HandleFileActivationOnAppInstanceActivated(storageFile));
                return;
            }

            // check the database for the audiobook
            var audiobook = await Repository.Audiobooks.GetByFilePathAsync(storageFile.Path);

            // if filepath doesn't match, then we need its metadata to check if it's in the database
            if (audiobook == null)
            {
                var metadata = storageFile.GetAudiobookSearchParameters();
                audiobook = await Repository.Audiobooks.GetByTitleAuthorComposerAsync(metadata.Title, metadata.Author,
                    metadata.Composer);

                // if the audiobook is not in the database, then we need to import it
                if (audiobook == null)
                {
                    await ViewModel.ImportAudiobookFromFileActivationAsync(storageFile.Path, false);
                    return;
                }
            }

            // if the audiobook is already playing, then we don't need to do anything
            if (audiobook.IsNowPlaying) return;

            // we need to get the currently playing audiobook and set it to not playing
            var nowPlayingAudiobook = await Repository.Audiobooks.GetNowPlayingAsync();
            if (nowPlayingAudiobook != null)
            {
                nowPlayingAudiobook.IsNowPlaying = false;
                await Repository.Audiobooks.UpsertAsync(nowPlayingAudiobook);
            }

            // set file activated audiobook to now playing
            audiobook.IsNowPlaying = true;
            await Repository.Audiobooks.UpsertAsync(audiobook);
        }
        catch (Exception e)
        {
            ViewModel.EnqueueNotification(new Notification
            {
                Message = "An error occurred while trying to open the file.",
                Severity = InfoBarSeverity.Error
            });
            ViewModel.LoggingService.LogError(e, true);

            if (onAppInstanceActivated)
                await DialogService.ShowErrorDialogAsync("File Activation Error", e.Message);
            else
                ViewModel.FileActivationError = e.Message;
        }
    }

    private async void HandleFileActivationOnAppInstanceActivated(IStorageFile storageFile)
    {
        // need to refresh the audiobook list in case any filters or searches have been applied
        await ViewModel.GetAudiobookListAsync();

        var searchParameters = storageFile.GetAudiobookSearchParameters();
        var audiobook = ViewModel.Audiobooks.FirstOrDefault(a => a.Title == searchParameters.Title &&
                                                                 a.Author == searchParameters.Author &&
                                                                 a.Narrator == searchParameters.Composer);

        // set the current position
        if (audiobook == null)
        {
            await ViewModel.ImportAudiobookFromFileActivationAsync(storageFile.Path, false);
            return;
        }

        // if the audiobook is already playing, then we don't need to do anything
        if (audiobook.IsNowPlaying) return;

        // if the audiobook is not playing, then we need to set the current position
        await PlayerViewModel.OpenAudiobook(audiobook);
    }

    // note: this is only called when audibly is already running and a file is opened
    private async void OnAppInstanceActivated(object? sender, AppActivationArguments e)
    {
        var mainInstance = AppInstance.FindOrRegisterForKey("main");

        if (e.Kind is ExtendedActivationKind.File && e.Data is IFileActivatedEventArgs fileActivatedEventArgs &&
            fileActivatedEventArgs.Files.FirstOrDefault() is IStorageFile storageFile)
        {
            await _dispatcherQueue.EnqueueAsync(() => HandleFileActivation(storageFile, true));

            // Bring the window to the foreground... first get the window handle...
            var hwnd = (HWND)WindowNative.GetWindowHandle(Window);

            // Restore window if minimized... requires Microsoft.Windows.CsWin32 NuGet package and a NativeMethods.txt file with ShowWindow method
            Windows.Win32.PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_RESTORE);

            // And call SetForegroundWindow... requires Microsoft.Windows.CsWin32 NuGet package and a NativeMethods.txt file with SetForegroundWindow method
            Windows.Win32.PInvoke.SetForegroundWindow(hwnd);
        }
    }

    private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        ViewModel.LoggingService.Log(e.Exception.Message);
    }

    /// <summary>
    ///     Configures the app to use the Sqlite data source. If no existing Sqlite database exists,
    ///     loads a demo database filled with fake data so the app has content.
    /// </summary>
    private static void UseSqlite()
    {
        var dbPath = ApplicationData.Current.LocalFolder.Path + @"\Audibly.db";
        var dbOptions = new DbContextOptionsBuilder<AudiblyContext>()
            .UseSqlite("Data Source=" + dbPath)
            .Options;

        // NOTE: for manual testing
        // UserSettings.Version = "2.0.15.0";
        // UserSettings.DataMigrationFailed = false;

        // check for current version key
        var userCurrentVersion = UserSettings.PreviousVersion = UserSettings.Version;
        var dataMigrationFailed = UserSettings.ShowDataMigrationFailedDialog;

        ViewModel.LoggingService.Log($"User's current version: {userCurrentVersion}");

        // check if user current version is less than 2.1.0
        if (userCurrentVersion != null &&
            Constants.CompareVersions(userCurrentVersion, Constants.DatabaseMigrationVersion) == -1 &&
            !dataMigrationFailed)
            try
            {
                // if the user's version is less than v2.1, then we need to update the database to the current version
                // this is a breaking change, so we need to reset the database and then re-import their data
                // make a copy of the current database
                var dbCopyPath = ApplicationData.Current.LocalFolder.Path + @"\Audibly.db.bak";
                File.Copy(dbPath, dbCopyPath, true);

                // NOTE: for manual testing: need to remove this line
                // dbPath = ApplicationData.Current.LocalFolder.Path + @"\Audibly_2015.db";
                var baseConnectionString = "Data Source=" + dbPath;
                var connectionString = new SqliteConnectionStringBuilder(baseConnectionString)
                {
                    Mode = SqliteOpenMode.ReadOnly
                }.ToString();

                List<Audiobooks> audiobooks;
                using (var connection = new SqliteConnection(connectionString))
                {
                    audiobooks = connection.Query<Audiobooks>("SELECT * FROM Audiobooks").ToList();
                }

                var audiobookExport = new List<ImportedAudiobook>();
                foreach (var audiobook in audiobooks)
                {
                    var importedAudiobook = new ImportedAudiobook
                    {
                        CurrentTimeMs = audiobook.CurrentTimeMs,
                        CoverImagePath = audiobook.CoverImagePath,
                        FilePath = audiobook.FilePath,
                        CurrentChapterIndex = audiobook.CurrentChapterIndex,
                        IsNowPlaying = audiobook.IsNowPlaying
                    };
                    var currentPositionSeconds = audiobook.CurrentTimeMs.ToSeconds();
                    importedAudiobook.Progress = Math.Ceiling(currentPositionSeconds / audiobook.Duration * 100);
                    importedAudiobook.IsCompleted = importedAudiobook.Progress >= 99.9;

                    audiobookExport.Add(importedAudiobook);
                }

                var json = JsonSerializer.Serialize(audiobookExport);

                var folder = ApplicationData.Current.LocalFolder;
                var file = folder.CreateFileAsync("audibly_export.audibly", CreationCollisionOption.ReplaceExisting)
                    .GetAwaiter().GetResult();
                FileIO.WriteTextAsync(file, json).GetAwaiter().GetResult();

                // set flag that data migration is required
                UserSettings.NeedToImportAudiblyExport = true;

                // delete the old database
                using (var context = new AudiblyContext(dbOptions))
                {
                    context.Database.EnsureDeleted();
                }

                // create the new database
                using (var context = new AudiblyContext(dbOptions))
                {
                    var databaseFacade = new DatabaseFacade(context);
                    if (databaseFacade.GetPendingMigrations().Any()) databaseFacade.Migrate();
                }

                Repository = new SqlAudiblyRepository(dbOptions);
            }
            catch (Exception e)
            {
                ViewModel.LoggingService.LogError(e, true);

                // delete the old database
                using (var context = new AudiblyContext(dbOptions))
                {
                    context.Database.EnsureDeleted();
                }

                // if there's an error, then we need to just start fresh with the new database
                using (var context = new AudiblyContext(dbOptions))
                {
                    var databaseFacade = new DatabaseFacade(context);
                    if (databaseFacade.GetPendingMigrations().Any()) databaseFacade.Migrate();
                }

                Repository = new SqlAudiblyRepository(dbOptions);

                // let user know that the data migration failed but that they can re-attempt it by going to
                // settings->advanced settings->re-attempt data migration
                UserSettings.NeedToImportAudiblyExport = false;
                UserSettings.ShowDataMigrationFailedDialog = true;
            }
        else
            try
            {
                // create the db context
                using (var context = new AudiblyContext(dbOptions))
                {
                    var databaseFacade = new DatabaseFacade(context);
                    if (databaseFacade.GetPendingMigrations().Any()) databaseFacade.Migrate();
                }

                Repository = new SqlAudiblyRepository(dbOptions);
            }
            catch (Exception e)
            {
                ViewModel.LoggingService.LogError(e, true);
                Repository = new SqlAudiblyRepository(dbOptions);
            }
    }

    public static void RestartApp()
    {
        var restartError = AppInstance.Restart("themeChanged");

        switch (restartError)
        {
            case AppRestartFailureReason.RestartPending:
                ViewModel.EnqueueNotification(new Notification
                {
                    Message = "Another restart is currently pending.",
                    Severity = InfoBarSeverity.Error
                });
                break;
            case AppRestartFailureReason.InvalidUser:
                ViewModel.EnqueueNotification(new Notification
                {
                    Message = "Restart failed: Invalid user.",
                    Severity = InfoBarSeverity.Error
                });
                break;
            case AppRestartFailureReason.Other:
                ViewModel.EnqueueNotification(new Notification
                {
                    Message = "Restart failed: Unknown error.",
                    Severity = InfoBarSeverity.Error
                });
                break;
        }
    }

    public static TEnum GetEnum<TEnum>(string text) where TEnum : struct
    {
        if (!typeof(TEnum).GetTypeInfo().IsEnum)
            throw new InvalidOperationException("Generic parameter 'TEnum' must be an enum.");
        return (TEnum)Enum.Parse(typeof(TEnum), text);
    }
}