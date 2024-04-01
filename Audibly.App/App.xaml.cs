// Author: rstewa · https://github.com/rstewa
// Created: 3/5/2024
// Updated: 3/22/2024

using System.Diagnostics;
using Windows.Globalization;
using Windows.Storage;
using Audibly.App.Helpers;
using Audibly.App.Services;
using Audibly.App.ViewModels;
using Audibly.App.Views;
using Audibly.Repository;
using Audibly.Repository.Interfaces;
using Audibly.Repository.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

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

        // Get theme choice from LocalSettings.
        var value = ApplicationData.Current.LocalSettings.Values["themeSetting"];

        if (value != null)
            // Apply theme choice.
            Current.RequestedTheme = value.ToString() == "Light" ? ApplicationTheme.Light : ApplicationTheme.Dark;
    }

    /// <summary>
    ///     Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Window = new MainWindow();

        win32WindowHelper = new Win32WindowHelper(Window);
        win32WindowHelper.SetWindowMinMaxSize(new Win32WindowHelper.POINT { x = 800, y = 800 });

        Window.Activate();

        UseSqlite();

        var shell = Window.Content as AppShell ?? new AppShell();
        shell.Language = ApplicationLanguages.Languages[0];
        Window.Content = shell;

        if (shell.AppFrame.Content == null)
            // When the navigation stack isn't restored, navigate to the first page
            // suppressing the initial entrance animation.
            shell.AppFrame.Navigate(typeof(Library), null,
                new SuppressNavigationTransitionInfo());
            // shell.AppFrame.Navigate(typeof(LibraryPage), null,
            //     new SuppressNavigationTransitionInfo());

        Window.Activate();
        
        MainRoot = shell.Content as FrameworkElement;
    }

    /// <summary>
    ///     Configures the app to use the Sqlite data source. If no existing Sqlite database exists,
    ///     loads a demo database filled with fake data so the app has content.
    /// </summary>
    private static void UseSqlite()
    {
        // string demoDatabasePath = Package.Current.InstalledLocation.Path + @"\Assets\Contoso.db";
        // string databasePath = ApplicationData.Current.LocalFolder.Path + @"\Contoso.db";

        // var folder = Environment.SpecialFolder.LocalApplicationData;
        // var path = Environment.GetFolderPath(folder);
        // var dbPath = System.IO.Path.Join(path, "audibly.db");

        var dbPath = ApplicationData.Current.LocalFolder.Path + @"\Audibly.db";

#if DEBUG
        Debug.WriteLine($"Database path: {dbPath}");
#endif

        // if (!File.Exists(databasePath))
        // {
        //     File.Copy(demoDatabasePath, databasePath);
        // }
        var dbOptions = new DbContextOptionsBuilder<AudiblyContext>().UseSqlite(
            "Data Source=" + dbPath);
        Repository = new SqlAudiblyRepository(dbOptions);
    }
}