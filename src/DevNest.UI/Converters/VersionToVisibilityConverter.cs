using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace DevNest.UI.Converters
{
    public class VersionToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string version)
            {

                return (version != null && version != "")
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
