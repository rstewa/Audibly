// Author: rstewa · https://github.com/rstewa
// Updated: 01/26/2025

using System;
using Audibly.App.Extensions;
using Microsoft.UI.Xaml.Data;

namespace Audibly.App.Helpers.Converters;

public class ProgressSliderValueConverter : IValueConverter
{
    #region IValueConverter Members

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var chapterPositionMs = (double)value;
        return chapterPositionMs.ToStr_ms();
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

    #endregion
}