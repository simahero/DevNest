using DevNest.Core.Models;
using System.Collections.ObjectModel;

namespace DevNest.Core.State
{
    public class AppState : IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private SettingsManager? _settingsManager;
        private ServiceManager? _serviceManager;
        private SiteManager? _siteManager;

        public SettingsModel? Settings { get; private set; }

        public ObservableCollection<SiteModel> Sites { get; private set; } = new();
        public ObservableCollection<SiteDefinition> AvailableSites { get; private set; } = new();

        public ObservableCollection<ServiceModel> Services { get; private set; } = new();
        public ObservableCollection<ServiceDefinition> AvailableServices { get; private set; } = new();

        public AppState(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task LoadAsync()
        {
            _settingsManager ??= (SettingsManager)_serviceProvider.GetService(typeof(SettingsManager))!;
            Settings = await _settingsManager.LoadSettingsAsync();

            _serviceManager ??= (ServiceManager)_serviceProvider.GetService(typeof(ServiceManager))!;

            var services = await _serviceManager.GetServicesAsync();
            Services.Clear();
            foreach (var service in services)
            {
                Services.Add(service);
            }

            var availableService = await _serviceManager.GetAvailableServices();
            AvailableServices.Clear();
            foreach (var service in availableService)
            {
                AvailableServices.Add(service);
            }

            _siteManager ??= (SiteManager)_serviceProvider.GetService(typeof(SiteManager))!;

            var sites = await _siteManager.GetSitesAsync();
            Sites.Clear();
            foreach (var site in sites)
            {
                Sites.Add(site);
            }

            var availableSites = await _siteManager.GetAvailableSitesAsync();
            AvailableSites.Clear();
            foreach (var site in availableSites)
            {
                AvailableSites.Add(site);
            }
        }

        public async Task Reload()
        {
            await LoadAsync();
        }

        public async Task ReloadSettings()
        {
            _settingsManager ??= (SettingsManager)_serviceProvider.GetService(typeof(SettingsManager))!;
            Settings = await _settingsManager.LoadSettingsAsync();
        }

        public async Task ReloadServices()
        {
            _serviceManager ??= (ServiceManager)_serviceProvider.GetService(typeof(ServiceManager))!;
            var services = await _serviceManager.GetServicesAsync();

            Services.Clear();
            foreach (var service in services)
            {
                Services.Add(service);
            }
        }

        public async Task ReloadSites()
        {
            _siteManager ??= (SiteManager)_serviceProvider.GetService(typeof(SiteManager))!;
            var sites = await _siteManager.GetSitesAsync();

            Sites.Clear();
            foreach (var site in sites)
            {
                Sites.Add(site);
            }
        }

        public void Dispose()
        {

        }
    }
}
