using System;
using Microsoft.UI.Xaml.Data;

namespace Audibly.App.Helpers.Converters;

public sealed class PlaybackSpeedIndexConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        PlaybackSpeedPresets.ToIndex((double)value);

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        PlaybackSpeedPresets.FromIndex((double)value);
}

public sealed class PlaybackSpeedToolTipConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        $"{PlaybackSpeedPresets.FromIndex((double)value):0.##}×";

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
