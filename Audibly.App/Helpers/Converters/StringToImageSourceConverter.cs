// Author: rstewa · https://github.com/rstewa
// Updated: 03/02/2025

using System;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Audibly.App.Helpers.Converters;

public class StringToImageSourceConverter : IValueConverter
{
    #region IValueConverter Members

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not string path || string.IsNullOrEmpty(path))
            return new BitmapImage(new Uri("ms-appx:///Assets/DefaultCoverImage.png"));
        
        try
        {
            return new BitmapImage(new Uri(path));
        }
        catch
        {
            return new BitmapImage(new Uri("ms-appx:///Assets/DefaultCoverImage.png"));
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

    #endregion
}