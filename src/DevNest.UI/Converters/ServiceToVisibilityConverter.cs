using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace DevNest.UI.Converters
{
    public class ServiceToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string serviceName && parameter is string targetService)
            {
                return string.Equals(serviceName, targetService, StringComparison.OrdinalIgnoreCase)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
