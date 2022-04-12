using Windows.Graphics;
using Windows.UI;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace Audibly.Extensions;

public static class WindowExtensions
{
    public static void SetWindowSize(this Window window, int width, int height,
        bool isResizable = true, bool extendsContentIntoTitleBar = true,
        bool isMinimizable = true, bool isMaximizable = true)
    {
        var hWnd = WindowNative.GetWindowHandle(window);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        var presenter = appWindow.Presenter as OverlappedPresenter;

        appWindow.TitleBar.ExtendsContentIntoTitleBar = extendsContentIntoTitleBar;
        appWindow.TitleBar.ButtonBackgroundColor = Color.FromArgb(1, 18, 18, 18);
        appWindow.TitleBar.ButtonInactiveBackgroundColor = Color.FromArgb(1, 18, 18, 18);
        presenter!.IsResizable = isResizable;
        presenter!.IsMaximizable = isMaximizable;
        presenter!.IsMinimizable = isMinimizable;
        appWindow.Resize(new SizeInt32 { Width = width, Height = height });
    }
}