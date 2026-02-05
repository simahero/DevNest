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
        private readonly PlatformServiceFactory _platformServiceFactory;


        private ISiteRepository? _siteRepository;
        private IServiceRepository? _serviceRepository;

        [ObservableProperty]
        private SettingsModel? _settings;

        public ObservableCollection<SiteModel> Sites { get; } = new();
        public ObservableCollection<SiteDefinition> AvailableSites { get; } = new();

        public ObservableCollection<ServiceModel> Services { get; } = new();
        public ObservableCollection<ServiceDefinition> AvailableServices { get; } = new();

        public AppState(IServiceProvider serviceProvider, PlatformServiceFactory platformServiceFactory, ISettingsRepository settingsRepository)
        {
            _serviceProvider = serviceProvider;
            _platformServiceFactory = platformServiceFactory;
            _settingsRepository = settingsRepository;
        }

        public async Task LoadAsync()
        {
            await LoadSettingsAsync();

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

        public async Task ReloadServices()
        {
            await LoadServicesAsync();
            await LoadAvailableServicesAsync();

            OnPropertyChanged(nameof(Services));
            OnPropertyChanged(nameof(AvailableServices));
        }

        public async Task ReloadSites()
        {
            await LoadSitesAsync();
            await LoadAvailableSitesAsync();

            OnPropertyChanged(nameof(Sites));
            OnPropertyChanged(nameof(AvailableSites));
        }

        public async Task LoadSettingsAsync()
        {
            Settings = await _settingsRepository.GetSettingsAsync();
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
            if (_serviceRepository == null || Settings == null) return;

            var services = await _serviceRepository.GetServicesAsync(Settings);
            Services.Clear();
            foreach (var service in services)
            {
                Services.Add(service);
            }
        }

        public async Task LoadAvailableServicesAsync()
        {
            if (_serviceRepository == null || Settings == null) return;

            var availableServices = await _serviceRepository.GetAvailableServicesAsync(Settings);
            AvailableServices.Clear();
            foreach (var service in availableServices)
            {
                AvailableServices.Add(service);
            }
        }

        public async Task LoadServiceVersions()
        {
            if (Settings != null)
            {
                await _settingsRepository.PopulateServiceVersionsAsync(Settings, Services, AvailableServices);
                await _settingsRepository.PopulateCommandsAsync(Settings);
            }
        }

        public async Task LoadSelectedVersion()
        {
            if (Settings != null)
            {
                _settingsRepository.SetSelectedVersion(Settings, Services);
            }
            await Task.CompletedTask;
        }

        public async Task CreateSiteAsync(string siteDefinitionName, string siteName, IProgress<string>? progress = null)
        {
            if (_siteRepository == null)
                throw new InvalidOperationException("SiteRepository is not initialized. Call LoadAsync first.");
            if (Settings == null)
                throw new InvalidOperationException("Settings are not loaded.");

            await _siteRepository.CreateSiteAsync(Settings, siteDefinitionName, siteName, progress);
            await LoadSitesAsync();
        }

        public void Dispose()
        {
            // Individual repositories are managed by DI container
        }
    }
}
