using Microsoft.UI.Xaml.Data;
using System;

namespace DevNest.UI.Converters
{
    /// <summary>
    /// Converter to check if count > 0 for enabling controls
    /// </summary>
    public class CountToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int count)
            {
                return count > 0;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
