using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevNest.Core.Enums;
using DevNest.Core.Helpers;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
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
        private readonly IPlatformServiceFactory _platformServiceFactory;


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

        public AppState AppState => _appState;

        public EnvironmentsViewModel(AppState appState, IPlatformServiceFactory platformServiceFactory)
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
        public Task Load()
        {
            return Task.CompletedTask;
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

            var settings = _appState.Settings;
            if (settings == null)
            {
                SetInstallationStatus(serviceType, "Settings not loaded.");
                return;
            }

            object? serviceModel = serviceType switch
            {
                ServiceType.Apache => settings.Apache,
                ServiceType.Nginx => settings.Nginx,
                ServiceType.PHP => settings.PHP,
                ServiceType.MySQL => settings.MySQL,
                ServiceType.PostgreSQL => settings.PostgreSQL,
                ServiceType.MongoDB => settings.MongoDB,
                ServiceType.Node => settings.Node,
                ServiceType.Redis => settings.Redis,
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
                UpdateVersionCollections(serviceType, serviceDefinition);
            }
            catch (Exception ex)
            {
                SetInstallationStatus(serviceType, $"Installation failed: {ex.Message}");
            }
            finally
            {
                SetInstallationProgress(serviceType, false, false, null);
            }
        }

        private void UpdateVersionCollections(ServiceType serviceType, ServiceDefinition installedService)
        {
            var settings = _appState.Settings;
            if (settings == null) return;
            object? collection = serviceType switch
            {
                ServiceType.Apache => settings.Apache,
                ServiceType.Nginx => settings.Nginx,
                ServiceType.PHP => settings.PHP,
                ServiceType.MySQL => settings.MySQL,
                ServiceType.PostgreSQL => settings.PostgreSQL,
                ServiceType.MongoDB => settings.MongoDB,
                ServiceType.Node => settings.Node,
                ServiceType.Redis => settings.Redis,
                _ => null
            };

            if (collection == null) return;

            dynamic dynCollection = collection;

            dynCollection.InstallableVersions?.Remove(installedService);

            if (dynCollection.AvailableVersions != null && !dynCollection.AvailableVersions.Contains(installedService.Name))
            {
                dynCollection.AvailableVersions.Add(installedService.Name);
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
