// Author: rstewa · https://github.com/rstewa
// Created: 3/21/2024
// Updated: 3/22/2024

using Microsoft.UI.Xaml;

namespace Audibly.App.ViewModels;

/// <summary>
///     Provides static methods for use in x:Bind function binding to convert bound values to the required value.
/// </summary>
public static class Converters
{
    /// <summary>
    ///     Returns the reverse of the provided value.
    /// </summary>
    public static bool Not(bool value)
    {
        return !value;
    }

    /// <summary>
    ///     Returns true if the specified value is not null; otherwise, returns false.
    /// </summary>
    public static bool IsNotNull(object value)
    {
        return value != null;
    }

    /// <summary>
    ///     Returns Visibility.Collapsed if the specified value is true; otherwise, returns Visibility.Visible.
    /// </summary>
    public static Visibility CollapsedIf(bool value)
    {
        return value ? Visibility.Collapsed : Visibility.Visible;
    }

    public static Visibility CollapsedIfNot(bool value)
    {
        return value ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    ///     Returns Visibility.Collapsed if the specified value is null; otherwise, returns Visibility.Visible.
    /// </summary>
    public static Visibility CollapsedIfNull(object value)
    {
        return value == null ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>
    ///     Returns Visibility.Collapsed if the specified string is null or empty; otherwise, returns Visibility.Visible.
    /// </summary>
    public static Visibility CollapsedIfNullOrEmpty(string value)
    {
        return string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;
    }
}