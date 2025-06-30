using CommunityToolkit.Mvvm.Input;
using DevNest.Core;
using DevNest.Core.Files;
using DevNest.Core.Models;
using DevNest.UI.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DevNest.UI.ViewModels
{
    public partial class DashboardViewModel : BaseViewModel
    {
        private readonly ServiceManager _serviceManager;
        private readonly SiteManager _siteManager;

        public ObservableCollection<ServiceModel> InstalledServices { get; } = new();

        public DashboardViewModel(ServiceManager serviceManager, SiteManager siteManager)
        {
            _serviceManager = serviceManager;
            _siteManager = siteManager;
            Title = "Dashboard";
        }

        [RelayCommand]
        private async Task LoadDashboardDataAsync()
        {
            if (InstalledServices.Count == 0)
            {
                await RefreshDashboardAsync();
            }
        }

        [RelayCommand]
        private async Task RefreshDashboardAsync()
        {
            IsLoading = true;
            try
            {
                var servicesTask = _serviceManager.GetServicesAsync();

                await Task.WhenAll(servicesTask);

                var services = servicesTask.Result.ToList();

                InstalledServices.Clear();

                var selectedServices = services
                    .Where(s => !string.IsNullOrEmpty(s.ServiceType) && s.IsSelected)
                    .ToList();

                foreach (var service in selectedServices)
                {
                    InstalledServices.Add(service);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading dashboard data: {ex.Message}");
                // TODO: Show error to user
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task ToggleServiceAsync(ServiceModel? service)
        {
            if (service == null) return;

            try
            {
                // Toggle the service using the manager
                await _serviceManager.ToggleServiceAsync(service.Name);

                // Get the updated service from the manager
                var updatedService = await _serviceManager.GetServiceAsync(service.Name);
                if (updatedService != null)
                {
                    // Update the properties of the existing instance to keep bindings
                    service.Status = updatedService.Status;
                    service.Process = updatedService.Process;
                    service.IsLoading = updatedService.IsLoading;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error toggling service {service.Name}: {ex.Message}");
                // TODO: Show error to user
            }
        }

        [RelayCommand]
        private void OpenLog()
        {
            var pathManager = ServiceLocator.GetService<PathManager>();
            var logPath = Path.Combine(pathManager.LogsPath, "debug.log");
            if (File.Exists(logPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = logPath,
                    UseShellExecute = true
                });
            }
            else
            {
                // Optionally show a message to the user
            }
        }

        protected override async Task OnLoadedAsync()
        {
            await LoadDashboardDataAsync();
        }
    }
}
