using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace DevNest.UI.Converters
{
    /// <summary>
    /// Converter to convert null values to Visibility enum values
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
