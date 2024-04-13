// Author: rstewa · https://github.com/rstewa
// Created: 4/3/2024
// Updated: 4/4/2024

using Windows.UI.ViewManagement;
using Audibly.App.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Audibly.App.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class PlayerPage : Page
{
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    /// <summary>
    ///     Gets the app-wide ViewModel instance.
    /// </summary>
    public MainViewModel ViewModel => App.ViewModel;

    /// <summary>
    ///     Gets the app-wide PlayerViewModel instance.
    /// </summary>
    public PlayerViewModel PlayerViewModel => App.PlayerViewModel;

    public PlayerPage()
    {
        InitializeComponent();
        
        // Set the title bar for the current view
        App.Window.SetTitleBar(NowPlayingPageTitleBar);
        App.Window.ExtendsContentIntoTitleBar = true;
    }
}