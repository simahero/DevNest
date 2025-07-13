using DevNest.Core.Models;

namespace DevNest.Core.Interfaces
{

    public interface ISettingsRepository
    {
        SettingsModel? Settings { get; }

        Task<SettingsModel> GetSettingsAsync();
        Task PopulateServiceVersionsAsync(IEnumerable<ServiceModel> installedServices, IEnumerable<ServiceDefinition> availableServices);
        Task PopulateCommandsAsync();
        void SetSelectedVersion(IEnumerable<ServiceModel> installedServices);
    }
}
