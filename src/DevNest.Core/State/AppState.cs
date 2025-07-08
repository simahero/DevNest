using CommunityToolkit.Mvvm.ComponentModel;
using DevNest.Core.Enums;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using System.Collections.ObjectModel;

namespace DevNest.Core.State
{
    public partial class AppState : ObservableObject, IDisposable
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly ISiteRepository _siteRepository;
        private readonly IServiceRepository _serviceRepository;

        public SettingsModel Settings => _settingsRepository.Settings!;

        public ObservableCollection<SiteModel> Sites { get; } = new();
        public ObservableCollection<SiteDefinition> AvailableSites { get; } = new();

        public ObservableCollection<ServiceModel> Services { get; } = new();
        public ObservableCollection<ServiceDefinition> AvailableServices { get; } = new();

        public AppState(
            ISettingsRepository settingsRepository,
            ISiteRepository siteRepository,
            IServiceRepository serviceRepository)
        {
            _settingsRepository = settingsRepository;
            _siteRepository = siteRepository;
            _serviceRepository = serviceRepository;
        }

        public async Task LoadAsync()
        {
            await LoadSettingsAsync();

            await LoadSitesAsync();
            await LoadAvailableSitesAsync();

            await LoadServicesAsync();
            await LoadAvailableServicesAsync();

            await LoadServiceVersions();
        }

        public async Task Reload() => await LoadAsync();

        public async Task LoadSettingsAsync()
        {
            await _settingsRepository.GetSettingsAsync();
        }

        public async Task LoadSitesAsync()
        {
            var sites = await _siteRepository.GetSitesAsync();
            Sites.Clear();
            foreach (var site in sites)
            {
                Sites.Add(site);
            }
        }

        public async Task LoadAvailableSitesAsync()
        {
            var availableSites = await _siteRepository.GetAvailableSitesAsync();
            AvailableSites.Clear();
            foreach (var site in availableSites)
            {
                AvailableSites.Add(site);
            }
        }

        public async Task LoadServicesAsync()
        {
            var services = await _serviceRepository.GetServicesAsync();
            Services.Clear();
            foreach (var service in services)
            {
                Services.Add(service);
            }
        }

        public async Task LoadAvailableServicesAsync()
        {
            var availableServices = await _serviceRepository.GetAvailableServicesAsync();
            AvailableServices.Clear();
            foreach (var service in availableServices)
            {
                AvailableServices.Add(service);
            }
        }

        public async Task LoadServiceVersions()
        {
            await _settingsRepository.PopulateServiceVersionsAsync(Services, AvailableServices);
        }

        public async Task CreateSiteAsync(string siteDefinitionName, string siteName, IProgress<string>? progress = null)
        {
            await _siteRepository.CreateSiteAsync(siteDefinitionName, siteName, progress);
            await LoadSitesAsync();
        }

        public void Dispose()
        {
            // Individual repositories are managed by DI container
        }
    }
}
