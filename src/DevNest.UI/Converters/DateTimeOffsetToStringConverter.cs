using System;
using Microsoft.UI.Xaml.Data;

namespace DevNest.UI.Converters
{
    public class DateTimeOffsetToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTimeOffset dto)
            {
                return dto.ToString("yyyy-MM-dd HH:mm");
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is string s && DateTimeOffset.TryParse(s, out var dto))
                return dto;
            return default(DateTimeOffset);
        }
    }
}
