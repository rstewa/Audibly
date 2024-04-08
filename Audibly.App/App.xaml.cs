// Author: rstewa · https://github.com/rstewa
// Created: 3/29/2024
// Updated: 4/5/2024

using System;
using System.Diagnostics;
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
using System.Runtime.InteropServices.WindowsRuntime;
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
    public static MainViewModel ViewModel { get; } = new(new M4BFileImportService(), new AppDataService());

    /// <summary>
    ///     Gets the app-wide PlayerViewModel singleton instance.
    /// </summary>
    public static PlayerViewModel PlayerViewModel { get; } = new();

    /// <summary>
    ///     Pipeline for interacting with backend service or database.
    /// </summary>
    public static IAudiblyRepository Repository { get; private set; }

    public static FrameworkElement MainRoot { get; private set; }

    /// <summary>
    ///     Initializes the singleton application object.  This is the first line of authored code
    ///     executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();
        this.UnhandledException += OnUnhandledException; 

        // Get theme choice from LocalSettings.
        var value = ApplicationData.Current.LocalSettings.Values["themeSetting"];

        if (value != null)
            // Apply theme choice.
            Current.RequestedTheme = value.ToString() == "Light" ? ApplicationTheme.Light : ApplicationTheme.Dark;
    }

    private async void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        // todo: log exception
    }
    /// <summary>
    ///     Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Window = WindowHelper.CreateWindow();

#if DEBUG
        Window.SizeChanged += (o, args) =>
        {
            Debug.WriteLine($"Main Window -> Width: {args.Size.Width}, Height: {args.Size.Height}");
        };
#endif

        // win32WindowHelper = new Win32WindowHelper(Window);
        // win32WindowHelper.SetWindowMinMaxSize(new Win32WindowHelper.POINT { x = 1500, y = 800 });

        UseSqlite();

        var shell = Window.Content as AppShell ?? new AppShell();
        shell.Language = ApplicationLanguages.Languages[0];
        Window.Content = shell;

        if (shell.AppFrame.Content == null)
            // When the navigation stack isn't restored, navigate to the first page
            // suppressing the initial entrance animation.
            shell.AppFrame.Navigate(typeof(LibraryCardPage), null,
                new SuppressNavigationTransitionInfo());

        Window.CustomizeWindow(-1, -1, true, true, true, true, true, true);

        Window.Activate();

        MainRoot = shell.Content as FrameworkElement;
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
}