using DevNest.Core.Models;

namespace DevNest.Core.Interfaces
{

    public interface IServiceRepository
    {
        Task<IEnumerable<ServiceModel>> GetServicesAsync(SettingsModel settings);
        Task<IEnumerable<ServiceDefinition>> GetAvailableServicesAsync(SettingsModel settings);
    }
}
