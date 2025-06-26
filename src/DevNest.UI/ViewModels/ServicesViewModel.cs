using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using DevNest.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace DevNest.UI.ViewModels
{
    public partial class ServicesViewModel : BaseViewModel
    {
        private readonly ServiceManager _serviceManager;
        private readonly InstallManager _installManager;
        private readonly IServicesReader _servicesReader;

        [ObservableProperty]
        private string _installationStatus = string.Empty;

        [ObservableProperty]
        private bool _isInstalling;

        [ObservableProperty]
        private bool _showInstallationPanel;

        [ObservableProperty]
        private string? _selectedServiceType;

        [ObservableProperty]
        private string? _selectedVersion;

        public ObservableCollection<ServiceModel> Services { get; } = new();
        public ObservableCollection<ServiceModel> InstalledServices { get; } = new();
        public ObservableCollection<ServiceDefinition> AvailableServices { get; } = new();
        public ObservableCollection<string> AvailableServiceTypes { get; } = new();
        public ObservableCollection<string> AvailableVersions { get; } = new();

        public IAsyncRelayCommand LoadServicesCommand { get; }
        public IAsyncRelayCommand LoadInstalledServicesCommand { get; }
        public IAsyncRelayCommand LoadAvailableServicesCommand { get; }
        public IAsyncRelayCommand InstallServiceCommand { get; }
        public IAsyncRelayCommand<ServiceModel> OpenServiceFolderCommand { get; }


        public ServicesViewModel(ServiceManager serviceManager, IServicesReader servicesReader, InstallManager installManager)
        {
            _serviceManager = serviceManager;
            _servicesReader = servicesReader;
            _installManager = installManager;
            Title = "Services";
            LoadServicesCommand = new AsyncRelayCommand(LoadServicesAsync);
            LoadInstalledServicesCommand = new AsyncRelayCommand(LoadInstalledServicesAsync);
            LoadAvailableServicesCommand = new AsyncRelayCommand(LoadAvailableServicesAsync);
            InstallServiceCommand = new AsyncRelayCommand(InstallServiceAsync);
            OpenServiceFolderCommand = new AsyncRelayCommand<ServiceModel>(OpenServiceFolderAsync);
        }

        public async Task LoadServicesAsync()
        {
            IsLoading = true;
            try
            {
                var services = await _serviceManager.GetServicesAsync();
                Services.Clear();
                foreach (var service in services)
                {
                    Services.Add(service);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading services: {ex.Message}");
                // TODO: Show error to user
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadAvailableServicesAsync()
        {
            IsLoading = true;
            try
            {
                var services = await _servicesReader.LoadAvailableServicesAsync();
                AvailableServices.Clear();
                foreach (var service in services)
                {
                    AvailableServices.Add(service);
                }

                AvailableServiceTypes.Clear();

                foreach (var service in services)
                {
                    if (!AvailableServiceTypes.Contains(service.ServiceType))
                    {
                        AvailableServiceTypes.Add(service.ServiceType);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading available services: {ex.Message}");
                // TODO: Show error to user
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadInstalledServicesAsync()
        {
            IsLoading = true;
            try
            {
                var services = await _servicesReader.LoadInstalledServicesAsync();
                InstalledServices.Clear();
                foreach (var service in services)
                {
                    InstalledServices.Add(service);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading installed services: {ex.Message}");
                // TODO: Show error to user
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task InstallServiceAsync()
        {
            if (SelectedServiceType == null || SelectedVersion == null)
            {
                return;
            }

            var selectedService = AvailableServices
                .FirstOrDefault(s => s.ServiceType == SelectedServiceType && s.Name.Equals(SelectedVersion));

            if (selectedService == null)
            {
                InstallationStatus = "Selected service not found.";
                return;
            }

            IsInstalling = true;
            ShowInstallationPanel = true;
            InstallationStatus = "Installing service...";

            try
            {
                var progress = new Progress<string>(message =>
                {
                    InstallationStatus = message;
                });

                var result = await _installManager.InstallServiceAsync(selectedService, progress);

                if (result.Success)
                {
                    InstallationStatus = result.Message;

                    await LoadInstalledServicesAsync();

                    SelectedServiceType = null;
                    SelectedVersion = null;
                }
                else
                {
                    InstallationStatus = result.Message;
                }
            }
            catch (Exception ex)
            {
                InstallationStatus = $"Installation failed: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(InstallationStatus);
            }
            finally
            {
                IsInstalling = false;
            }
        }

        private async Task OpenServiceFolderAsync(ServiceModel? service)
        {
            if (service == null) return;

            try
            {
                await Windows.System.Launcher.LaunchFolderPathAsync(service.Path);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening service folder: {ex.Message}");
                // TODO: Show error to user
            }
        }
        protected override async Task OnLoadedAsync()
        {
            await LoadServicesCommand.ExecuteAsync(null);
            await LoadInstalledServicesCommand.ExecuteAsync(null);
            await LoadAvailableServicesCommand.ExecuteAsync(null);
        }

        partial void OnSelectedServiceTypeChanged(string? value)
        {
            AvailableVersions.Clear();
            foreach (var service in AvailableServices)
            {
                if (!AvailableVersions.Contains(service.Name) &&
                    service.ServiceType == SelectedServiceType &&
                    !InstalledServices.Any(installedService => installedService.Name == service.Name))
                {
                    AvailableVersions.Add(service.Name);
                }
            }
        }
    }

}
