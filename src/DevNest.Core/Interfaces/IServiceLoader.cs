using DevNest.Core.Models;

namespace DevNest.Core.Interfaces
{
    public interface IServiceLoader
    {
        Task<IEnumerable<ServiceModel>> GetServicesAsync();
        Task<IEnumerable<ServiceDefinition>> GetAvailableServices();
    }
}
