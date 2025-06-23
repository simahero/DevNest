using Microsoft.Extensions.DependencyInjection;
using System;

namespace DevNest.UI.Services
{
    public static class ServiceLocator
    {
        private static IServiceProvider? _serviceProvider;

        public static void SetServiceProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public static T GetService<T>() where T : class
        {
            if (_serviceProvider == null)
                throw new InvalidOperationException("Service provider not initialized. Call SetServiceProvider first.");

            return _serviceProvider.GetRequiredService<T>();
        }

        public static object GetService(Type serviceType)
        {
            if (_serviceProvider == null)
                throw new InvalidOperationException("Service provider not initialized. Call SetServiceProvider first.");

            return _serviceProvider.GetRequiredService(serviceType);
        }
    }
}
