// Author: rstewa · https://github.com/rstewa
// Created: 04/15/2024
// Updated: 10/03/2024

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Audibly.App.Extensions;
using Audibly.App.Helpers;
using Audibly.App.Services;
using Audibly.App.ViewModels;
using Audibly.Repository.Interfaces;
using Audibly.Repository.Sql;
using CommunityToolkit.WinUI;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.AppLifecycle;
using Sentry;
using WinRT.Interop;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;

namespace Audibly.App;

/// <summary>
///     Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private static Win32WindowHelper win32WindowHelper;

    /// <summary>
    ///     Gets main App Window
    /// </summary>
    public static Window Window { get; private set; }

    /// <summary>
    ///     Gets the app-wide MainViewModel singleton instance.
    /// </summary>
    public static MainViewModel ViewModel { get; } =
        new(new FileImportService(), new AppDataService(), new MessageService(),
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

    /// <summary>
    ///     Initializes the singleton application object.  This is the first line of authored code
    ///     executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        // get dns from app.config

        SentrySdk.Init(options =>
        {
            // Tells which project in Sentry to send events to:
            options.Dsn = Helpers.Sentry.Dsn;

            options.AutoSessionTracking = true;

            // Set traces_sample_rate to 1.0 to capture 100% of transactions for tracing.
            // We recommend adjusting this value in production.
            options.TracesSampleRate = 1.0;

            // Enable Global Mode since this is a client app.
            options.IsGlobalModeEnabled = true;

            // TODO:Any other Sentry options you need go here.
        });

        InitializeComponent();
        UnhandledException += OnUnhandledException;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        ViewModel.LoggingService.Log(e.Exception.Message);
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
        win32WindowHelper.SetWindowMinMaxSize(new Win32WindowHelper.POINT { x = 1600, y = 800 });

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

    private async void HandleFileActivation(IStorageFile storageFile)
    {
        ViewModel.LoggingService.Log($"File activated: {storageFile.Path}");

        var audiobook = ViewModel.Audiobooks.FirstOrDefault(a => a.CurrentSourceFile.FilePath == storageFile.Path);

        // set the current position
        if (audiobook == null)
        {
            await ViewModel.ImportAudiobookFromFileActivationAsync(storageFile.Path, false);
        }
    }

    private async void OnAppInstanceActivated(object? sender, AppActivationArguments e)
    {
        var mainInstance = AppInstance.FindOrRegisterForKey("main");

        if (e.Kind is ExtendedActivationKind.File && e.Data is IFileActivatedEventArgs fileActivatedEventArgs &&
            fileActivatedEventArgs.Files.FirstOrDefault() is IStorageFile storageFile)
        {
            await _dispatcherQueue.EnqueueAsync(() => HandleFileActivation(storageFile));

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
        
        using (var context = new AudiblyContext(dbOptions))
        {
            var databaseFacade = new DatabaseFacade(context);
            
            if(databaseFacade.GetPendingMigrations().Any()){
                databaseFacade.Migrate();
            }
        }

        Repository = new SqlAudiblyRepository(dbOptions);
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

    public static string Version
    {
        get
        {
            var version = Assembly.GetEntryAssembly()?.GetName().Version;
            return version != null
                ? $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}"
                : string.Empty;
        }
    }
}