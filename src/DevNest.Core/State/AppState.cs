using CommunityToolkit.Mvvm.ComponentModel;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using DevNest.Core.Repositories;
using DevNest.Core.Services;
using System.Collections.ObjectModel;

namespace DevNest.Core.State
{
    public partial class AppState : ObservableObject, IDisposable
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly IServiceProvider _serviceProvider;


        private ISiteRepository? _siteRepository;
        private IServiceRepository? _serviceRepository;
        private PlatformServiceFactory? _platformServiceFactory;

        public SettingsModel Settings => _settingsRepository.Settings!;

        public ObservableCollection<SiteModel> Sites { get; } = new();
        public ObservableCollection<SiteDefinition> AvailableSites { get; } = new();

        public ObservableCollection<ServiceModel> Services { get; } = new();
        public ObservableCollection<ServiceDefinition> AvailableServices { get; } = new();

        public AppState(ISettingsRepository settingsRepository, IServiceProvider serviceProvider)
        {
            _settingsRepository = settingsRepository;
            _serviceProvider = serviceProvider;
        }

        public async Task LoadAsync()
        {
            await LoadSettingsAsync();

            _platformServiceFactory = new PlatformServiceFactory(_serviceProvider, _settingsRepository);

            if (_settingsRepository is SettingsRepository concreteSettingsRepo)
            {
                concreteSettingsRepo.SetPlatformServiceFactory(_platformServiceFactory);
            }

            _siteRepository = new SiteRepository(_platformServiceFactory);
            _serviceRepository = new ServiceRepository(_platformServiceFactory);

            await LoadServicesAsync();
            await LoadAvailableServicesAsync();

            await LoadSitesAsync();
            await LoadAvailableSitesAsync();

            await LoadServiceVersions();

            OnPropertyChanged(nameof(Sites));
            OnPropertyChanged(nameof(AvailableSites));
            OnPropertyChanged(nameof(Services));
            OnPropertyChanged(nameof(AvailableServices));
            OnPropertyChanged(nameof(Settings));

        }

        public async Task Reload() => await LoadAsync();

        public async Task LoadSettingsAsync()
        {
            await _settingsRepository.GetSettingsAsync();
        }

        public async Task LoadSitesAsync()
        {
            if (_siteRepository == null) return;

            var sites = await _siteRepository.GetSitesAsync();
            Sites.Clear();
            foreach (var site in sites)
            {
                Sites.Add(site);
            }
        }

        public async Task LoadAvailableSitesAsync()
        {
            if (_siteRepository == null) return;

            var availableSites = await _siteRepository.GetAvailableSitesAsync();
            AvailableSites.Clear();
            foreach (var site in availableSites)
            {
                AvailableSites.Add(site);
            }
        }

        public async Task LoadServicesAsync()
        {
            if (_serviceRepository == null) return;

            var services = await _serviceRepository.GetServicesAsync();
            Services.Clear();
            foreach (var service in services)
            {
                Services.Add(service);
            }
        }

        public async Task LoadAvailableServicesAsync()
        {
            if (_serviceRepository == null) return;

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
            await _settingsRepository.PopulateCommandsAsync();
        }

        public async Task LoadSelectedVersion()
        {
            await _settingsRepository.SetSelectedVersion(Services);
        }

        public async Task CreateSiteAsync(string siteDefinitionName, string siteName, IProgress<string>? progress = null)
        {
            if (_siteRepository == null)
                throw new InvalidOperationException("SiteRepository is not initialized. Call LoadAsync first.");

            await _siteRepository.CreateSiteAsync(siteDefinitionName, siteName, progress);
            await LoadSitesAsync();
        }

        public void Dispose()
        {
            // Individual repositories are managed by DI container
        }
    }
}
