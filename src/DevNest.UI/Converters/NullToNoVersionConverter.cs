using Microsoft.UI.Xaml.Data;
using System;

namespace DevNest.UI.Converters
{
    public class NullToNoVersionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string version)
            {
                return string.IsNullOrEmpty(version) ? "No version" : version;
            }
            return "No version";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
