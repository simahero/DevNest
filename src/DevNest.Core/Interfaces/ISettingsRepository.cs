using DevNest.Core.Models;

namespace DevNest.Core.Interfaces
{

    public interface ISettingsRepository
    {
        Task<SettingsModel> GetSettingsAsync();
        Task PopulateServiceVersionsAsync(SettingsModel settings, IEnumerable<ServiceModel> installedServices, IEnumerable<ServiceDefinition> availableServices);
        Task PopulateCommandsAsync(SettingsModel settings);
        void SetSelectedVersion(SettingsModel settings, IEnumerable<ServiceModel> installedServices);
    }
}
