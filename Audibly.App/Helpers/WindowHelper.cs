// Author: rstewa · https://github.com/rstewa
// Updated: 01/26/2025

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace Audibly.App.Helpers;

// Helper class to allow the app to find the Window that contains an
// arbitrary UIElement (GetWindowForElement).  To do this, we keep track
// of all active Windows.  The app code must call WindowHelper.CreateWindow
// rather than "new Window" so we can keep track of all the relevant
// windows.  In the future, we would like to support this in platform APIs.
public static class WindowHelper
{
    // public static List<Window, string> ActiveWindows { get; } = new();
    public static Dictionary<string, Window> ActiveWindows { get; } = new();

    public static Window CreateWindow(string name)
    {
        // var newWindow = new Window
        // {
        //     SystemBackdrop = new MicaBackdrop()
        // };
        var newWindow = new Window();
        TrackWindow(name, newWindow);
        return newWindow;
    }

    // TODO: having the names hardcoded is gross
    public static Window? GetMainWindow()
    {
        ActiveWindows.TryGetValue("MainWindow", out var mainWindow);
        return mainWindow;
    }

    public static Window? GetMiniPlayerWindow()
    {
        ActiveWindows.TryGetValue("MiniPlayerWindow", out var miniPlayerWindow);
        return miniPlayerWindow;
    }

    public static void HideMiniPlayer()
    {
        ActiveWindows.TryGetValue("MiniPlayerWindow", out var miniPlayerWindow);
        if (miniPlayerWindow == null) return;
        (miniPlayerWindow.AppWindow.Presenter as OverlappedPresenter)?.Minimize();
        miniPlayerWindow.AppWindow.IsShownInSwitchers = false;
    }

    public static void CloseMiniPlayer()
    {
        ActiveWindows.TryGetValue("MiniPlayerWindow", out var miniPlayerWindow);
        if (miniPlayerWindow == null) return;
        miniPlayerWindow.Close();
    }

    public static void CloseAll()
    {
        foreach (var window in ActiveWindows)
            window.Value.Close();
    }

    public static void RestoreMainWindow()
    {
        ActiveWindows.TryGetValue("MainWindow", out var mainWindow);
        if (mainWindow == null) return;
        (mainWindow.AppWindow.Presenter as OverlappedPresenter)?.Restore();
        mainWindow.AppWindow.IsShownInSwitchers = true;
    }

    public static void HideMainWindow()
    {
        ActiveWindows.TryGetValue("MainWindow", out var mainWindow);
        if (mainWindow == null) return;
        (mainWindow.AppWindow.Presenter as OverlappedPresenter)?.Minimize();
        mainWindow.AppWindow.IsShownInSwitchers = false;
    }

    public static void TrackWindow(string name, Window window)
    {
        window.Closed += (_, _) => { ActiveWindows.Remove(name); };
        ActiveWindows[name] = window;
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
                if (element.XamlRoot == window.Value.Content.XamlRoot)
                    return window.Value;
        return null;
    }

    // get dpi for an element
    public static double GetRasterizationScaleForElement(UIElement element)
    {
        if (element.XamlRoot != null)
            foreach (var window in ActiveWindows)
                if (element.XamlRoot == window.Value.Content.XamlRoot)
                    return element.XamlRoot.RasterizationScale;
        return 0.0;
    }

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