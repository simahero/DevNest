using CommunityToolkit.Mvvm.Input;
using DevNest.Core.Models;
using DevNest.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace DevNest.UI.ViewModels
{
    public partial class DashboardViewModel : BaseViewModel
    {
        private readonly ServiceManager _serviceManager;
        private readonly SiteManager _siteManager;

        public ObservableCollection<ServiceModel> InstalledServices { get; } = new();

        public IAsyncRelayCommand LoadDashboardDataCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }
        public IAsyncRelayCommand<ServiceModel> ToggleServiceCommand { get; }

        public DashboardViewModel(ServiceManager serviceManager, SiteManager siteManager)
        {
            _serviceManager = serviceManager;
            _siteManager = siteManager;
            Title = "Dashboard";
            LoadDashboardDataCommand = new AsyncRelayCommand(LoadDashboardDataAsync);
            RefreshCommand = new AsyncRelayCommand(RefreshDashboardAsync);
            ToggleServiceCommand = new AsyncRelayCommand<ServiceModel>(ToggleServiceAsync);
        }

        public async Task LoadDashboardDataAsync()
        {
            IsLoading = true;
            try
            {
                var servicesTask = _serviceManager.GetServicesAsync();
                var sitesTask = _siteManager.GetInstalledSitesAsync();

                await Task.WhenAll(servicesTask, sitesTask);

                var services = servicesTask.Result.ToList();
                var sites = sitesTask.Result.ToList();

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

        private async Task RefreshDashboardAsync()
        {
            await LoadDashboardDataAsync();
        }

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

        protected override async Task OnLoadedAsync()
        {
            await LoadDashboardDataCommand.ExecuteAsync(null);
        }
    }
}
