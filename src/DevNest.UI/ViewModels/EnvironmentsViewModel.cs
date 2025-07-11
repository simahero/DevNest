using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevNest.Core.Enums;
using DevNest.Core.Helpers;
using System.Collections.ObjectModel;
using DevNest.Core.Models;
using DevNest.Core.Services;
using DevNest.Core.State;
using DevNest.UI.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DevNest.UI.ViewModels
{
    public partial class EnvironmentsViewModel : BaseViewModel
    {
        private readonly AppState _appState;
        private readonly PlatformServiceFactory _platformServiceFactory;


        [ObservableProperty]
        public ServiceInstallationStatus _apacheStatus = new();
        [ObservableProperty]
        public ServiceInstallationStatus _nginxStatus = new();
        [ObservableProperty]
        public ServiceInstallationStatus _pHPStatus = new();
        [ObservableProperty]
        public ServiceInstallationStatus _mySQLStatus = new();
        [ObservableProperty]
        public ServiceInstallationStatus _postgreSQLStatus = new();
        [ObservableProperty]
        public ServiceInstallationStatus _mongoDBStatus = new();
        [ObservableProperty]
        public ServiceInstallationStatus _nodeStatus = new();
        [ObservableProperty]
        public ServiceInstallationStatus _redisStatus = new();

        public SettingsModel? Settings => _appState.Settings;

        public ObservableCollection<ServiceModel> Services => _appState.Services;
        public ObservableCollection<ServiceDefinition> AvailableServices => _appState.AvailableServices;

        public EnvironmentsViewModel(AppState appState, PlatformServiceFactory platformServiceFactory)
        {
            _appState = appState;
            _platformServiceFactory = platformServiceFactory;
            Title = "Environments";
        }

        [RelayCommand]
        private void OpenBin()
        {
            var binPath = Path.Combine(PathHelper.BinPath);
            if (Directory.Exists(binPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = binPath,
                    UseShellExecute = true
                });
            }
        }

        [RelayCommand]
        private void OpenConfig()
        {
            var configPath = PathHelper.ConfigPath;
            if (Directory.Exists(configPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = configPath,
                    UseShellExecute = true
                });
            }
        }

        [RelayCommand]
        private void OpenData()
        {
            var dataPath = PathHelper.DataPath;
            if (Directory.Exists(dataPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = dataPath,
                    UseShellExecute = true
                });
            }
        }

        [RelayCommand]
        private void OpenEtc()
        {
            var etcPath = Path.Combine(PathHelper.EtcPath);
            if (Directory.Exists(etcPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = etcPath,
                    UseShellExecute = true
                });
            }
        }

        [RelayCommand]
        private void OpenPHPIni()
        {
            if (_appState.Settings?.PHP?.Version != null)
            {
                var phpIniPath = Path.Combine(PathHelper.BinPath, "PHP", _appState.Settings.PHP.Version, "php.ini");
                if (File.Exists(phpIniPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = phpIniPath,
                        UseShellExecute = true
                    });
                }
            }
        }

        [RelayCommand]
        private void OpenPHPExtensionsWindow()
        {
            if (_appState.Settings?.PHP?.Version != null)
            {
                var phpIniPath = Path.Combine(PathHelper.BinPath, "PHP", _appState.Settings.PHP.Version, "php.ini");

                var extWindow = new PHPExtensionWindow(phpIniPath);
                extWindow.Activate();
            }
        }

        private void SetInstallationProgress(ServiceType serviceType, bool? isInstalling, bool? showPanel, string? status)
        {
            var statusObj = GetStatusObject(serviceType);
            if (isInstalling.HasValue) statusObj.IsInstalling = isInstalling.Value;
            if (showPanel.HasValue) statusObj.ShowInstallationPanel = showPanel.Value;
            if (status != null) statusObj.InstallationStatus = status;
        }

        private void SetInstallationStatus(ServiceType serviceType, string status)
        {
            SetInstallationProgress(serviceType, null, null, status);
        }

        [RelayCommand]
        public async Task Load()
        {
            await Reload();
        }

        private async Task Reload()
        {
            await _appState.Reload();
        }

        [RelayCommand]
        private async Task InstallServiceAsync(object serviceTypeParam)
        {
            if (serviceTypeParam == null) return;

            ServiceType serviceType;
            if (serviceTypeParam is ServiceType enumValue)
            {
                serviceType = enumValue;
            }
            else if (serviceTypeParam is string stringValue && Enum.TryParse<ServiceType>(stringValue, out var parsedValue))
            {
                serviceType = parsedValue;
            }
            else
            {
                return;
            }

            var version = GetStatusObject(serviceType).SelectedVersion;
            if (string.IsNullOrEmpty(version))
            {
                return;
            }

            if (Settings == null)
            {
                SetInstallationStatus(serviceType, "Settings not loaded.");
                return;
            }

            object? serviceModel = serviceType switch
            {
                ServiceType.Apache => Settings.Apache,
                ServiceType.Nginx => Settings.Nginx,
                ServiceType.PHP => Settings.PHP,
                ServiceType.MySQL => Settings.MySQL,
                ServiceType.PostgreSQL => Settings.PostgreSQL,
                ServiceType.MongoDB => Settings.MongoDB,
                ServiceType.Node => Settings.Node,
                ServiceType.Redis => Settings.Redis,
                _ => null
            };

            if (serviceModel == null)
            {
                SetInstallationStatus(serviceType, $"Service model not found for {serviceType}.");
                return;
            }

            dynamic dynModel = serviceModel;
            var installableVersionsObj = dynModel.InstallableVersions;
            IEnumerable<ServiceDefinition>? installableVersions = null;
            if (installableVersionsObj is IEnumerable<ServiceDefinition> directCast)
            {
                installableVersions = directCast;
            }
            else if (installableVersionsObj is IEnumerable<object> objEnum)
            {
                try
                {
                    installableVersions = objEnum.OfType<ServiceDefinition>().ToList();
                }
                catch
                {
                    SetInstallationStatus(serviceType, $"No installable versions for {serviceType}.");
                    return;
                }
            }
            else
            {
                SetInstallationStatus(serviceType, $"No installable versions for {serviceType}.");
                return;
            }

            var serviceDefinition = installableVersions?.FirstOrDefault(s => s.Name == version);

            if (serviceDefinition == null)
            {
                SetInstallationStatus(serviceType, $"Service not found for {serviceType} version {version}");
                return;
            }

            SetInstallationProgress(serviceType, true, true, $"Installing {serviceType} {version}...");

            try
            {
                var progress = new Progress<string>(message =>
                {
                    SetInstallationStatus(serviceType, message);
                });

                var serviceInstaller = _platformServiceFactory.GetServiceInstaller();
                await serviceInstaller.InstallServiceAsync(serviceDefinition, progress);

                SetInstallationStatus(serviceType, $"{serviceType} {version} installed successfully.");
            }
            catch (Exception ex)
            {
                SetInstallationStatus(serviceType, $"Installation failed: {ex.Message}");
            }
            finally
            {
                SetInstallationProgress(serviceType, false, false, null);
                await Reload();
            }
        }

        private ServiceInstallationStatus GetStatusObject(ServiceType serviceType)
        {
            return serviceType switch
            {
                ServiceType.Apache => ApacheStatus,
                ServiceType.Nginx => NginxStatus,
                ServiceType.PHP => PHPStatus,
                ServiceType.MySQL => MySQLStatus,
                ServiceType.PostgreSQL => PostgreSQLStatus,
                ServiceType.MongoDB => MongoDBStatus,
                ServiceType.Node => NodeStatus,
                ServiceType.Redis => RedisStatus,
                _ => throw new ArgumentOutOfRangeException(nameof(serviceType), serviceType, null)
            };
        }

    }
}
