using CommunityToolkit.Mvvm.Input;
using DevNest.Core.Helpers;
using DevNest.Core.Models;
using DevNest.Core.Services;
using DevNest.Core.State;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace DevNest.UI.ViewModels
{
    public partial class DashboardViewModel : BaseViewModel
    {
        private readonly AppState _appState;
        private readonly PlatformServiceFactory _platformServiceFactory;

        public AppState AppState => _appState;

        public DashboardViewModel(AppState appState, PlatformServiceFactory platformServiceFactory)
        {
            _appState = appState;
            _platformServiceFactory = platformServiceFactory;
            Title = "Dashboard";
        }

        [RelayCommand]
        private async Task ToggleServiceAsync(ServiceModel? service)
        {
            if (service == null) return;

            try
            {
                if (_appState.Settings == null) throw new InvalidOperationException("Settings are not loaded.");
                var serviceRunner = _platformServiceFactory.GetServiceRunner(_appState.Settings);
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
    }
}
