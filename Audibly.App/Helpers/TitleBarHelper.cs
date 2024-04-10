// Author: rstewa · https://github.com/rstewa
// Created: 4/9/2024
// Updated: 4/9/2024

using Windows.UI;
using Microsoft.UI;
using Microsoft.UI.Xaml;

namespace Audibly.App.Helpers;

internal class TitleBarHelper
{
    // workaround as Appwindow titlebar doesn't update caption button colors correctly when changed while app is running
    // https://task.ms/44172495
    public static Color ApplySystemThemeToCaptionButtons(Window window)
    {
        // var frame = (Application.Current as Audibly.App.App).GetRootFrame() as FrameworkElement;
        var frame = App.MainRoot;
        Color color;
        if (frame.ActualTheme == ElementTheme.Dark)
            color = Colors.White;
        else
            color = Colors.Black;
        SetCaptionButtonColors(window, color);
        return color;
    }

    public static void SetCaptionButtonColors(Window window, Color color)
    {
        var res = Application.Current.Resources;
        res["WindowCaptionForeground"] = color;
        window.AppWindow.TitleBar.ButtonForegroundColor = color;
    }

    public static void SetCaptionButtonBackgroundColors(Window window, Color? color)
    {
        var titleBar = window.AppWindow.TitleBar;
        titleBar.ButtonBackgroundColor = color;
    }

    public static void SetForegroundColor(Window window, Color? color)
    {
        var titleBar = window.AppWindow.TitleBar;
        titleBar.ForegroundColor = color;
    }

    public static void SetBackgroundColor(Window window, Color? color)
    {
        var titleBar = window.AppWindow.TitleBar;
        titleBar.BackgroundColor = color;
    }
}