using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace Audibly.App.Converters
{
    public class CornerRadiusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double cornerRadiusDouble)
            {
                return new CornerRadius(cornerRadiusDouble);
            }
            return new CornerRadius(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is CornerRadius cornerRadius)
            {
                return $"{cornerRadius.TopLeft},{cornerRadius.TopRight},{cornerRadius.BottomRight},{cornerRadius.BottomLeft}";
            }
            return "0,0,0,0";
        }
    }
}
