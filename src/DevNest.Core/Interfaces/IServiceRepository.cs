using DevNest.Core.Models;

namespace DevNest.Core.Interfaces
{

    public interface IServiceRepository
    {
        Task<IEnumerable<ServiceModel>> GetServicesAsync();
        Task<IEnumerable<ServiceDefinition>> GetAvailableServicesAsync();
    }
}
