using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevNest.Core.Exceptions;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace DevNest.UI.ViewModels
{
    public partial class ServicesViewModel : BaseViewModel
    {
        private readonly IServiceManager _serviceManager;
        private readonly IServicesReader _servicesReader;
        private readonly IServiceInstallationService _installationService;

        [ObservableProperty]
        private ServiceDefinition? _selectedService;

        [ObservableProperty]
        private string _installationStatus = string.Empty;

        [ObservableProperty]
        private bool _isInstalling;

        [ObservableProperty]
        private bool _showInstallationPanel;

        public ObservableCollection<Service> Services { get; } = new();
        public ObservableCollection<InstalledService> InstalledServices { get; } = new();
        public ObservableCollection<ServiceDefinition> AvailableServices { get; } = new();

        public ServicesViewModel(IServiceManager serviceManager, IServicesReader servicesReader, IServiceInstallationService installationService)
        {
            _serviceManager = serviceManager;
            _servicesReader = servicesReader;
            _installationService = installationService;
            Title = "Services";
            LoadServicesCommand = new AsyncRelayCommand(LoadServicesAsync);
            LoadInstalledServicesCommand = new AsyncRelayCommand(LoadInstalledServicesAsync);
            LoadAvailableServicesCommand = new AsyncRelayCommand(LoadAvailableServicesAsync);
            InstallServiceCommand = new AsyncRelayCommand(InstallServiceAsync);
            StartServiceCommand = new AsyncRelayCommand<Service>(StartServiceAsync);
            StopServiceCommand = new AsyncRelayCommand<Service>(StopServiceAsync);
            OpenServiceFolderCommand = new AsyncRelayCommand<InstalledService>(OpenServiceFolderAsync);
            RefreshCommand = new AsyncRelayCommand(RefreshServicesAsync);
        }

        public IAsyncRelayCommand LoadServicesCommand { get; }
        public IAsyncRelayCommand LoadInstalledServicesCommand { get; }
        public IAsyncRelayCommand LoadAvailableServicesCommand { get; }
        public IAsyncRelayCommand InstallServiceCommand { get; }
        public IAsyncRelayCommand<Service> StartServiceCommand { get; }
        public IAsyncRelayCommand<Service> StopServiceCommand { get; }
        public IAsyncRelayCommand<InstalledService> OpenServiceFolderCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }

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

        private async Task StartServiceAsync(Service? service)
        {
            if (service == null) return;

            try
            {
                await _serviceManager.StartServiceAsync(service.Name);
            }
            catch (ServiceException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error starting service: {ex.Message}");
                // TODO: Show error to user
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected error starting service: {ex.Message}");
                // TODO: Show error to user
            }
        }

        private async Task StopServiceAsync(Service? service)
        {
            if (service == null) return;

            try
            {
                await _serviceManager.StopServiceAsync(service.Name);
            }
            catch (ServiceException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping service: {ex.Message}");
                // TODO: Show error to user
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected error stopping service: {ex.Message}");
                // TODO: Show error to user
            }
        }

        private async Task RefreshServicesAsync()
        {
            IsLoading = true;
            try
            {
                await _serviceManager.RefreshServicesAsync();
                await LoadServicesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing services: {ex.Message}");
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
            if (SelectedService == null) return;

            IsInstalling = true;
            ShowInstallationPanel = true;
            InstallationStatus = "Installing service...";

            try
            {
                var progress = new Progress<string>(message =>
                {
                    InstallationStatus = message;
                });

                var result = await _installationService.InstallServiceAsync(SelectedService, progress);

                if (result.Success)
                {
                    InstallationStatus = result.Message;
                    // Refresh the installed services list
                    await LoadInstalledServicesAsync();
                    // Reset selection
                    SelectedService = null;
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

        private async Task OpenServiceFolderAsync(InstalledService? service)
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
    }
}
