using CommunityToolkit.Mvvm.Input;
using DevNest.Core.Helpers;
using DevNest.Core.Models;
using DevNest.Core.Services;
using DevNest.Core.State;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace DevNest.UI.ViewModels
{
    public partial class DashboardViewModel : BaseViewModel
    {
        private readonly AppState _appState;
        private readonly PlatformServiceFactory _platformServiceFactory;

        public ObservableCollection<ServiceModel> InstalledServices { get; } = new();

        public DashboardViewModel(AppState appState, PlatformServiceFactory platformServiceFactory)
        {
            _appState = appState;
            _platformServiceFactory = platformServiceFactory;
            Title = "Dashboard";

            // Subscribe to changes in the AppState Services collection
            _appState.Services.CollectionChanged += OnServicesCollectionChanged;
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
            await _appState.Reload();
            PopulateInstalledServices();
        }

        private void PopulateInstalledServices()
        {
            InstalledServices.Clear();
            foreach (var service in _appState.Services)
            {
                if (service.IsSelected)
                {
                    InstalledServices.Add(service);
                }
            }
        }

        private void OnServicesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            PopulateInstalledServices();
        }
    }
}
