using DevNest.Core.Models;
using System.Collections.ObjectModel;

namespace DevNest.Core.Interfaces
{

    public interface IAppStateRepository : IDisposable
    {
        // Settings
        SettingsModel? Settings { get; }
        Task<SettingsModel> GetSettingsAsync();
        Task UpdateSettingsAsync(SettingsModel settings);

        // Sites
        ObservableCollection<SiteModel> Sites { get; }
        ObservableCollection<SiteDefinition> AvailableSites { get; }
        Task RefreshSitesAsync();
        Task RefreshAvailableSitesAsync();

        // Services
        ObservableCollection<ServiceModel> Services { get; }
        ObservableCollection<ServiceDefinition> AvailableServices { get; }
        Task RefreshServicesAsync();
        Task RefreshAvailableServicesAsync();

        // Full refresh
        Task RefreshAllAsync();
    }
}
