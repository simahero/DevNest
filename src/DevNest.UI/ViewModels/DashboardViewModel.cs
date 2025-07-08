using CommunityToolkit.Mvvm.Input;
using DevNest.Core.Helpers;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using DevNest.Core.Services;
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
        private readonly IServiceRepository _serviceRepository;
        private readonly PlatformServiceFactory _platformServiceFactory;

        public ObservableCollection<ServiceModel> InstalledServices { get; } = new();

        public DashboardViewModel(IServiceRepository serviceRepository, PlatformServiceFactory platformServiceFactory)
        {
            _serviceRepository = serviceRepository;
            _platformServiceFactory = platformServiceFactory;
            Title = "Dashboard";
        }

        [RelayCommand]
        private async Task LoadDashboardDataAsync()
        {
            IsLoading = true;
            try
            {
                var services = await _serviceRepository.GetServicesAsync();

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

                foreach (var installed in InstalledServices.ToList())
                {
                    if (!selectedServices.Any(selected => selected.Name == installed.Name))
                    {
                        InstalledServices.Remove(installed);
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
                var serviceRunner = _platformServiceFactory.GetServiceRunner();
                await serviceRunner.ToggleServiceAsync(service);
            }
            catch (Exception)
            {
            }
        }

        [RelayCommand]
        private void OpenLog()
        {
            var logPath = Path.Combine(PathHelper.LogsPath);
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
