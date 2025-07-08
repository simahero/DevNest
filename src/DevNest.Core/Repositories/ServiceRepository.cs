using DevNest.Core.Enums;
using DevNest.Core.Events;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using DevNest.Core.Services;

namespace DevNest.Core.Repositories
{

    public class ServiceRepository : IServiceRepository
    {
        private readonly PlatformServiceFactory _platformServiceFactory;

        public ServiceRepository(PlatformServiceFactory platformServiceFactory)
        {
            _platformServiceFactory = platformServiceFactory;
        }

        public async Task<IEnumerable<ServiceModel>> GetServicesAsync()
        {
            try
            {
                var serviceLoader = _platformServiceFactory.GetServiceLoader();
                var services = await serviceLoader.GetServicesAsync();

                return services;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing services: {ex.Message}");
                return Enumerable.Empty<ServiceModel>();
            }
        }

        public async Task<IEnumerable<ServiceDefinition>> GetAvailableServicesAsync()
        {
            try
            {
                var serviceLoader = _platformServiceFactory.GetServiceLoader();
                return await serviceLoader.GetAvailableServices();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading available services: {ex.Message}");
                return Enumerable.Empty<ServiceDefinition>();
            }
        }
    }
}
