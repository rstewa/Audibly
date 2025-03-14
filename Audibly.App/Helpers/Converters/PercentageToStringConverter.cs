// Author: rstewa · https://github.com/rstewa
// Updated: 01/26/2025

using System;
using Microsoft.UI.Xaml.Data;

namespace Audibly.App.Helpers.Converters;

public class PercentageToStringConverter : IValueConverter
{
    #region IValueConverter Members

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double percentage) return $"{percentage}%";
        return "0%";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is string percentageString)
            if (percentageString.EndsWith("%"))
                return double.Parse(percentageString.TrimEnd('%'));
        return 0;
    }

    #endregion
}