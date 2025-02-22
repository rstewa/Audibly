// Author: rstewa · https://github.com/rstewa
// Updated: 01/26/2025

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Audibly.App.Helpers.Converters;

public class CornerRadiusConverter : IValueConverter
{
    #region IValueConverter Members

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double cornerRadiusDouble) return new CornerRadius(cornerRadiusDouble);
        return new CornerRadius(0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is CornerRadius cornerRadius)
            return
                $"{cornerRadius.TopLeft},{cornerRadius.TopRight},{cornerRadius.BottomRight},{cornerRadius.BottomLeft}";
        return "0,0,0,0";
    }

    #endregion
}