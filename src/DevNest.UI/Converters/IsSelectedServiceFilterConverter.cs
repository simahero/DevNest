using DevNest.Core.Models;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DevNest.UI.Converters
{
    public class IsSelectedServiceFilterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var services = value as IEnumerable<ServiceModel>;
            if (services == null) return new List<ServiceModel>();

            return services.ToList().Where(service => service.IsSelected);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
