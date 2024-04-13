// Author: rstewa · https://github.com/rstewa
// Created: 3/29/2024
// Updated: 4/9/2024

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Windows.ApplicationModel.Core;
using Windows.Globalization;
using Windows.Storage;
using Audibly.App.Extensions;
using Audibly.App.Helpers;
using Audibly.App.Services;
using Audibly.App.ViewModels;
using Audibly.App.Views;
using Audibly.Repository;
using Audibly.Repository.Interfaces;
using Audibly.Repository.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.AppLifecycle;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;

namespace Audibly.App;

/// <summary>
///     Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private static Win32WindowHelper win32WindowHelper;

    /// <summary>
    ///     Gets main App Window
    /// </summary>
    public static Window Window { get; private set; }

    /// <summary>
    ///     Gets the app-wide MainViewModel singleton instance.
    /// </summary>
    public static MainViewModel ViewModel { get; } =
        new(new M4BFileImportService(), new AppDataService(), new MessageService());

    /// <summary>
    ///     Gets the app-wide PlayerViewModel singleton instance.
    /// </summary>
    public static PlayerViewModel PlayerViewModel { get; } = new();

    /// <summary>
    ///     Pipeline for interacting with backend service or database.
    /// </summary>
    public static IAudiblyRepository Repository { get; private set; }

    public static FrameworkElement MainRoot { get; private set; }
    
    public static Frame? RootFrame { get; private set; }

    /// <summary>
    ///     Initializes the singleton application object.  This is the first line of authored code
    ///     executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
    }

    private async void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        // todo: log exception
    }

    /// <summary>
    ///     Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        Window = WindowHelper.CreateWindow();

        win32WindowHelper = new Win32WindowHelper(Window);
        win32WindowHelper.SetWindowMinMaxSize(new Win32WindowHelper.POINT { x = 1500, y = 800 });

        UseSqlite();

        // set now playing audiobook
        await ViewModel.GetAudiobookListAsync();
        var nowPlaying = ViewModel.Audiobooks.FirstOrDefault(a => a.IsNowPlaying);

        if (nowPlaying != null)
        {
            PlayerViewModel.NowPlaying = nowPlaying;
            PlayerViewModel.OpenAudiobook(nowPlaying);
        }

        // var shell = Window.Content as AppShell ?? new AppShell();
        // shell.Language = ApplicationLanguages.Languages[0];
        // Window.Content = shell;
        //
        // if (shell.AppFrame.Content == null)
        //     // When the navigation stack isn't restored, navigate to the first page
        //     // suppressing the initial entrance animation.
        //     shell.AppFrame.Navigate(typeof(LibraryCardPage), null,
        //         new SuppressNavigationTransitionInfo());

        RootFrame = Window.Content as Frame;
        
        if (RootFrame == null)
        {
            RootFrame = new Frame();
            RootFrame.NavigationFailed += OnNavigationFailed;
            Window.Content = RootFrame;
        }
        
        if (RootFrame.Content == null)
        {
            RootFrame.Navigate(typeof(AppShell), args.Arguments);
        }
        
        Window.CustomizeWindow(-1, -1, true, true, true, true, true, true);

        ThemeHelper.Initialize();
        
        Window.Activate();
    }

    private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Configures the app to use the Sqlite data source. If no existing Sqlite database exists,
    ///     loads a demo database filled with fake data so the app has content.
    /// </summary>
    private static void UseSqlite()
    {
        var dbPath = ApplicationData.Current.LocalFolder.Path + @"\Audibly.db";

#if DEBUG
        Debug.WriteLine($"Database path: {dbPath}");
#endif

        // TODO: add demo database
        // if (!File.Exists(databasePath))
        // {
        //     File.Copy(demoDatabasePath, databasePath);
        // }
        var dbOptions = new DbContextOptionsBuilder<AudiblyContext>().UseSqlite(
            "Data Source=" + dbPath);
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
            var version = Assembly.GetEntryAssembly().GetName().Version;
            return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
        }
    }
}