// Author: rstewa · https://github.com/rstewa
// Updated: 01/26/2025

using Windows.Storage;
using Microsoft.UI.Xaml;

namespace Audibly.App.Helpers;

/// <summary>
///     Class providing functionality around switching and restoring theme settings
/// </summary>
public static class ThemeHelper
{
    private const string SelectedAppThemeKey = "SelectedAppTheme";

    /// <summary>
    ///     Gets the current actual theme of the app based on the requested theme of the
    ///     root element, or if that value is Default, the requested theme of the Application.
    /// </summary>
    public static ElementTheme ActualTheme
    {
        get
        {
            foreach (var window in WindowHelper.ActiveWindows)
                if (window.Value.Content is FrameworkElement rootElement)
                    if (rootElement.RequestedTheme != ElementTheme.Default)
                        return rootElement.RequestedTheme;

            return App.GetEnum<ElementTheme>(Application.Current.RequestedTheme.ToString());
        }
    }

    /// <summary>
    ///     Gets or sets (with LocalSettings persistence) the RequestedTheme of the root element.
    /// </summary>
    public static ElementTheme RootTheme
    {
        get
        {
            foreach (var window in WindowHelper.ActiveWindows)
                if (window.Value.Content is FrameworkElement rootElement)
                    return rootElement.RequestedTheme;

            return ElementTheme.Default;
        }
        set
        {
            foreach (var window in WindowHelper.ActiveWindows)
                if (window.Value.Content is FrameworkElement rootElement)
                    rootElement.RequestedTheme = value;

            if (NativeHelper.IsAppPackaged)
                ApplicationData.Current.LocalSettings.Values[SelectedAppThemeKey] = value.ToString();
        }
    }

    public static void Initialize()
    {
        if (NativeHelper.IsAppPackaged)
        {
            var savedTheme = ApplicationData.Current.LocalSettings.Values[SelectedAppThemeKey]?.ToString();

            if (savedTheme != null)
            {
                RootTheme = App.GetEnum<ElementTheme>(savedTheme);
            }
            else
            {
                ApplicationData.Current.LocalSettings.Values[SelectedAppThemeKey] = ElementTheme.Dark.ToString();
                RootTheme = ElementTheme.Dark;
            }
        }
    }

    public static bool IsDarkTheme()
    {
        if (RootTheme == ElementTheme.Default) return Application.Current.RequestedTheme == ApplicationTheme.Dark;
        return RootTheme == ElementTheme.Dark;
    }
}