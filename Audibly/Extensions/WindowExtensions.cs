//   Author: Ryan Stewart
//   Date: 05/20/2022

using Windows.Graphics;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using static PInvoke.User32;
using System;

namespace Audibly.Extensions;

public static class WindowExtensions
{
    public static void SetWindowSize(this Window window, int width, int height)
    {
    }

    public static void SetWindowSize(this Window window, int width, int height,
        bool isResizable = true, bool extendsContentIntoTitleBar = true,
        bool isMinimizable = true, bool isMaximizable = true)
    {
        var hWnd = WindowNative.GetWindowHandle(window);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        var presenter = appWindow.Presenter as OverlappedPresenter;

        var scalingFactor = (float)GetDpiForWindow(hWnd) / 96;
        if(scalingFactor != 1)
        {
            width = Math.Ceiling(width * scalingFactor).ToInt();
            height = Math.Ceiling(height * scalingFactor).ToInt();
        }

        // appWindow.TitleBar.ExtendsContentIntoTitleBar = extendsContentIntoTitleBar;
        // appWindow.TitleBar.ButtonBackgroundColor = Color.FromArgb(1, 18, 18, 18);
        // appWindow.TitleBar.ButtonInactiveBackgroundColor = Color.FromArgb(1, 18, 18, 18);
        presenter!.IsResizable = isResizable;
        presenter!.IsMaximizable = isMaximizable;
        presenter!.IsMinimizable = isMinimizable;
        appWindow.Resize(new SizeInt32 { Width = width, Height = height });
    }
}