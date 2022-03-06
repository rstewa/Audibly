using System;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using static PInvoke.User32;

namespace Audibly;

/// <summary>
///     Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private Window _mWindow;

    /// <summary>
    ///     Initializes the singleton application object.  This is the first line of authored code
    ///     executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    ///     Invoked when the application is launched normally by the end user.  Other entry points
    ///     will be used such as when the application is launched to open a specific file.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _mWindow = new MainWindow();
        var hwnd = WindowNative.GetWindowHandle(_mWindow);

        SetWindowDetails(hwnd, 515, 715);
        _mWindow.Activate();
    }

    private static void SetWindowDetails(IntPtr hwnd, int width, int height)
    {
        var dpi = GetDpiForWindow(hwnd);
        var scalingFactor = (float)dpi / 96;
        width = (int)(width * scalingFactor);
        height = (int)(height * scalingFactor);

        // setting window size
        _ = SetWindowPos(
            hwnd, SpecialWindowHandles.HWND_TOP,
            0, 0, width, height,
            SetWindowPosFlags.SWP_NOMOVE
        );
        _ = SetWindowLong(
            hwnd,
            WindowLongIndexFlags.GWL_STYLE,
            (SetWindowLongFlags)(GetWindowLong(
                                     hwnd,
                                     WindowLongIndexFlags.GWL_STYLE
                                 ) &

                                 // ~(int)SetWindowLongFlags.WS_MINIMIZEBOX & // disables minimize button on window
                                 ~(int)SetWindowLongFlags.WS_MAXIMIZEBOX)
        ); // disables maximize button on window

        // setting window title
        _ = SetWindowText(hwnd, "Audibly");

        // todo: set application icon

        // disable resizing of the main window
        var myWndId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var apw = AppWindow.GetFromWindowId(myWndId);
        var presenter = apw.Presenter as OverlappedPresenter;

        presenter!.IsResizable = false;
    }
}