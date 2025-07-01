using DevNest.Core.Enums;
using Microsoft.UI.Xaml.Data;
using System;

namespace DevNest.UI.Converters
{
    public class ServiceStatusToActionTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is ServiceStatus status)
            {
                return status switch
                {
                    ServiceStatus.Running => "Running",
                    ServiceStatus.Starting => "Starting...",
                    ServiceStatus.Stopping => "Stopping...",
                    ServiceStatus.Stopped => "Stopped",
                    _ => "Stopped"
                };
            }

            if (value is bool isRunning)
            {
                return isRunning ? "Stop" : "Start";
            }

            return "Start";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
