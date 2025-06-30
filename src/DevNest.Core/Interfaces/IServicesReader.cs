using DevNest.Core.Models;

namespace DevNest.Core.Interfaces
{
    public interface IServicesReader
    {
        Task<List<ServiceModel>> LoadInstalledServicesAsync();
        Task<List<ServiceDefinition>> LoadAvailableServicesAsync();
    }
}
