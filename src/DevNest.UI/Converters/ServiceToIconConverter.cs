using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;

namespace DevNest.UI.Converters
{
    public class ServiceToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string serviceName && !string.IsNullOrWhiteSpace(serviceName))
            {
                var uri = new Uri($"ms-appx:///Assets/Logos/{serviceName.ToLowerInvariant()}.svg");
                return new SvgImageSource(uri);
            }

            return new SvgImageSource(new Uri("ms-appx:///Assets/Logos/default.svg"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
