using Microsoft.UI.Xaml.Data;
using System;

namespace DevNest.UI.Converters
{
    /// <summary>
    /// Converter to check if an object is not null for enabling controls
    /// </summary>
    public class ObjectToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
