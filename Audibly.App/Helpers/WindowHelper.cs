// Author: rstewa · https://github.com/rstewa
// Created: 3/21/2024
// Updated: 3/22/2024

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;

namespace Audibly.App.Helpers;

// Helper class to allow the app to find the Window that contains an
// arbitrary UIElement (GetWindowForElement).  To do this, we keep track
// of all active Windows.  The app code must call WindowHelper.CreateWindow
// rather than "new Window" so we can keep track of all the relevant
// windows.  In the future, we would like to support this in platform APIs.
public class WindowHelper
{
    public static Window CreateWindow()
    {
        // var newWindow = new Window
        // {
        //     SystemBackdrop = new MicaBackdrop()
        // };
        var newWindow = new Window();
        TrackWindow(newWindow);
        return newWindow;
    }

    public static void HideMainWindow()
    {
        var mainWindow = ActiveWindows.FirstOrDefault(w => w.Content is AppShell);
        if (mainWindow == null) return;
        (mainWindow.AppWindow.Presenter as OverlappedPresenter)?.Minimize();
        mainWindow.AppWindow.IsShownInSwitchers = false;
    }

    public static void TrackWindow(Window window)
    {
        window.Closed += (sender, args) => { ActiveWindows.Remove(window); };
        ActiveWindows.Add(window);
    }

    public static AppWindow GetAppWindow(Window window)
    {
        var hWnd = WindowNative.GetWindowHandle(window);
        var wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
        return AppWindow.GetFromWindowId(wndId);
    }

    public static Window GetWindowForElement(UIElement element)
    {
        if (element.XamlRoot != null)
            foreach (var window in ActiveWindows)
                if (element.XamlRoot == window.Content.XamlRoot)
                    return window;
        return null;
    }

    // get dpi for an element
    public static double GetRasterizationScaleForElement(UIElement element)
    {
        if (element.XamlRoot != null)
            foreach (var window in ActiveWindows)
                if (element.XamlRoot == window.Content.XamlRoot)
                    return element.XamlRoot.RasterizationScale;
        return 0.0;
    }

    public static List<Window> ActiveWindows { get; } = new();

    public static StorageFolder GetAppLocalFolder()
    {
        StorageFolder localFolder;
        if (!NativeHelper.IsAppPackaged)
            localFolder = Task.Run(async () => await StorageFolder.GetFolderFromPathAsync(AppContext.BaseDirectory))
                .Result;
        else
            localFolder = ApplicationData.Current.LocalFolder;
        return localFolder;
    }
}