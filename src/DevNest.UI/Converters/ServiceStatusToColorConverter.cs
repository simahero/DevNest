using DevNest.Core.Models;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace DevNest.UI.Converters
{
    public class ServiceStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is ServiceStatus status)
            {
                return status switch
                {
                    ServiceStatus.Running => new SolidColorBrush(Microsoft.UI.Colors.LawnGreen),
                    ServiceStatus.Starting => new SolidColorBrush(Microsoft.UI.Colors.Orange),
                    ServiceStatus.Stopping => new SolidColorBrush(Microsoft.UI.Colors.Orange),
                    ServiceStatus.Stopped => new SolidColorBrush(Microsoft.UI.Colors.Red),
                    _ => new SolidColorBrush(Microsoft.UI.Colors.Gray)
                };
            }

            if (value is bool isRunning)
            {
                return isRunning
                    ? new SolidColorBrush(Microsoft.UI.Colors.Green)
                    : new SolidColorBrush(Microsoft.UI.Colors.Red);
            }

            return new SolidColorBrush(Microsoft.UI.Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
