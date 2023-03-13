//   Author: Ryan Stewart
//   Date: 05/20/2022

using Windows.Graphics;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using static PInvoke.User32;
using System;
using System.Runtime.InteropServices;

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

    [DllImport("user32.dll")]
    static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);
    
    // [DllImport ( "user32.dll" )]
    // static extern int SetWindowLong ( IntPtr hWnd, int nIndex, uint dwNewLong );
    //
    // [DllImport ( "user32.dll" )]
    // static extern int GetWindowLong ( IntPtr hWnd, int nIndex);
    
    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_LAYERED = 0x80000;
    public const int LWA_ALPHA = 0x2;
    public const int LWA_COLORKEY = 0x1;
    
    public static void SetWindowTransparent(this Window window)
    {
        var hWnd = WindowNative.GetWindowHandle(window);
        // _ = SetWindowLong(hWnd, GWL_EXSTYLE, (uint)(GetWindowLong(hWnd, GWL_EXSTYLE) ^ WS_EX_LAYERED));
        SetLayeredWindowAttributes(hWnd, 0, 64, LWA_ALPHA);
    }
}