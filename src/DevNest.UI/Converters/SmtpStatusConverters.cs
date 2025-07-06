using System;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using System.Globalization;

namespace DevNest.UI.Converters
{
    public class SmtpStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isRunning)
                return new SolidColorBrush(isRunning ? Color.FromArgb(255, 76, 175, 80) : Color.FromArgb(255, 244, 67, 54)); // Green or Red
            return new SolidColorBrush(Color.FromArgb(255, 158, 158, 158)); // Gray
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class SmtpStatusToActionTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isRunning)
                return isRunning ? "Stop" : "Start";
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
