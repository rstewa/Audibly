using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Globalization;
using Windows.Storage;
using Audibly.App.Services;
using Audibly.App.ViewModels;
using Audibly.App.Views;
using Audibly.Repository;
using Audibly.Repository.Interfaces;
using Audibly.Repository.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml.Media.Animation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Audibly.App
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Gets main App Window
        /// </summary>
        public static Window Window { get { return m_window; } }
        private static Window m_window;
        
        /// <summary>
        /// Gets the app-wide MainViewModel singleton instance.
        /// </summary>
        public static MainViewModel ViewModel { get; } = new(new FileImportService());
        
        /// <summary>
        /// Pipeline for interacting with backend service or database.
        /// </summary>
        public static IAudiblyRepository Repository { get; private set; }
        
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App() => InitializeComponent();

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();
            
            UseSqlite();
            
            AppShell shell = m_window.Content as AppShell ?? new AppShell();
            shell.Language = ApplicationLanguages.Languages[0];
            m_window.Content = shell;

            if (shell.AppFrame.Content == null)
            {
                // When the navigation stack isn't restored, navigate to the first page
                // suppressing the initial entrance animation.
                shell.AppFrame.Navigate(typeof(AudiobookListPage), null,
                    new SuppressNavigationTransitionInfo());
            }

            m_window.Activate();
        }
        
        /// <summary>
        /// Configures the app to use the Sqlite data source. If no existing Sqlite database exists, 
        /// loads a demo database filled with fake data so the app has content.
        /// </summary>
        public static void UseSqlite()
        {
            // string demoDatabasePath = Package.Current.InstalledLocation.Path + @"\Assets\Contoso.db";
            // string databasePath = ApplicationData.Current.LocalFolder.Path + @"\Contoso.db";
            
            // var folder = Environment.SpecialFolder.LocalApplicationData;
            // var path = Environment.GetFolderPath(folder);
            // var dbPath = System.IO.Path.Join(path, "audibly.db");
            
            var dbPath = ApplicationData.Current.LocalFolder.Path + @"\Audibly.db";
            
            // if (!File.Exists(databasePath))
            // {
            //     File.Copy(demoDatabasePath, databasePath);
            // }
            var dbOptions = new DbContextOptionsBuilder<AudiblyContext>().UseSqlite(
                "Data Source=" + dbPath);
            Repository = new SqlAudiblyRepository(dbOptions);
        }
    }
}
