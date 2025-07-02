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

        public ObservableCollection<ServiceModel> InstalledServices { get; } = new();

        public DashboardViewModel(ServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
            Title = "Dashboard";
        }

        [RelayCommand]
        private async Task LoadDashboardDataAsync()
        {
            await RefreshDashboardAsync();
        }

        [RelayCommand]
        private async Task RefreshDashboardAsync()
        {
            IsLoading = true;
            try
            {
                var services = await _serviceManager.GetServicesAsync();

                var selectedServices = services.Where(s => !string.IsNullOrEmpty(s.ServiceType.ToString()) && s.IsSelected).ToList();

                foreach (var service in selectedServices)
                {
                    var existingService = InstalledServices.FirstOrDefault(existing => existing.Name == service.Name);
                    if (existingService == null)
                    {
                        InstalledServices.Add(service);
                    }
                    else
                    {
                        existingService.Command = service.Command;
                        existingService.IsSelected = service.IsSelected;
                        existingService.Path = service.Path;
                        existingService.WorkingDirectory = service.WorkingDirectory;
                    }
                }

            }
            catch (Exception)
            {

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
                await _serviceManager.ToggleServiceAsync(service);
            }
            catch (Exception)
            {
            }
        }

        [RelayCommand]
        private void OpenLog()
        {
            var logPath = Path.Combine(PathManager.LogsPath);
            if (Directory.Exists(logPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = logPath,
                    UseShellExecute = true
                });
            }
        }

        [RelayCommand]
        private void OpenPHPMyAdmin()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "http://localhost/phpmyadmin",
                UseShellExecute = true
            });
        }

        protected override async Task OnLoadedAsync()
        {
            await LoadDashboardDataAsync();
        }
    }
}
