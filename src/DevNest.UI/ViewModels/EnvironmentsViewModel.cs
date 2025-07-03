using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevNest.Core;
using DevNest.Core.Enums;
using DevNest.Core.Helpers;
using DevNest.Core.Models;
using DevNest.UI.Windows;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DevNest.UI.ViewModels
{
    public partial class EnvironmentsViewModel : BaseViewModel
    {
        private readonly SettingsManager _settingsManager;

        [ObservableProperty]
        private ApacheModel? _apache;

        [ObservableProperty]
        private MySQLModel? _mySQL;

        [ObservableProperty]
        private PHPModel? _php;

        [ObservableProperty]
        private NodeModel? _node;

        [ObservableProperty]
        private RedisModel? _redis;

        [ObservableProperty]
        private PostgreSQLModel? _postgreSQL;

        [ObservableProperty]
        private NginxModel? _nginx;

        [ObservableProperty]
        private MongoDBModel? _mongoDB;


        [ObservableProperty]
        private bool _isInstalling;

        [ObservableProperty]
        private bool _showInstallationPanel;

        [ObservableProperty]
        private string _installationStatus = string.Empty;

        // Service-specific installation progress properties
        [ObservableProperty]
        private bool _isInstallingApache;

        [ObservableProperty]
        private bool _showApacheInstallationPanel;

        [ObservableProperty]
        private string _apacheInstallationStatus = string.Empty;

        [ObservableProperty]
        private bool _isInstallingNginx;

        [ObservableProperty]
        private bool _showNginxInstallationPanel;

        [ObservableProperty]
        private string _nginxInstallationStatus = string.Empty;

        [ObservableProperty]
        private bool _isInstallingPhp;

        [ObservableProperty]
        private bool _showPhpInstallationPanel;

        [ObservableProperty]
        private string _phpInstallationStatus = string.Empty;

        [ObservableProperty]
        private bool _isInstallingMysql;

        [ObservableProperty]
        private bool _showMysqlInstallationPanel;

        [ObservableProperty]
        private string _mysqlInstallationStatus = string.Empty;

        [ObservableProperty]
        private bool _isInstallingPostgreSQL;

        [ObservableProperty]
        private bool _showPostgreSQLInstallationPanel;

        [ObservableProperty]
        private string _postgreSQLInstallationStatus = string.Empty;

        [ObservableProperty]
        private bool _isInstallingMongoDB;

        [ObservableProperty]
        private bool _showMongoDBInstallationPanel;

        [ObservableProperty]
        private string _mongoDBInstallationStatus = string.Empty;

        [ObservableProperty]
        private bool _isInstallingNode;

        [ObservableProperty]
        private bool _showNodeInstallationPanel;

        [ObservableProperty]
        private string _nodeInstallationStatus = string.Empty;

        [ObservableProperty]
        private bool _isInstallingRedis;

        [ObservableProperty]
        private bool _showRedisInstallationPanel;

        [ObservableProperty]
        private string _redisInstallationStatus = string.Empty;

        [ObservableProperty]
        private string? _selectedApacheVersion;

        [ObservableProperty]
        private string? _selectedNginxVersion;

        [ObservableProperty]
        private string? _selectedPhpVersion;

        [ObservableProperty]
        private string? _selectedMySQLVersion;

        [ObservableProperty]
        private string? _selectedPostgreSQLVersion;

        [ObservableProperty]
        private string? _selectedMongoDBVersion;

        [ObservableProperty]
        private string? _selectedNodeVersion;

        [ObservableProperty]
        private string? _selectedRedisVersion;

        public EnvironmentsViewModel(SettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
            Title = "Environments";
        }

        [RelayCommand]
        private void OpenSettings()
        {
            var settingsPath = PathHelper.SettingsPath;
            if (File.Exists(settingsPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = settingsPath,
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
            if (_settingsManager.CurrentSettings?.PHP?.Version != null)
            {
                var phpIniPath = Path.Combine(PathHelper.BinPath, "PHP", _settingsManager.CurrentSettings.PHP.Version, "php.ini");
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
            if (_settingsManager.CurrentSettings?.PHP?.Version != null)
            {
                var phpIniPath = Path.Combine(PathHelper.BinPath, "PHP", _settingsManager.CurrentSettings.PHP.Version, "php.ini");

                var extWindow = new PHPExtensionWindow(phpIniPath);
                extWindow.Activate();
            }
        }

        public override async Task LoadAsync()
        {
            await base.LoadAsync();
            var settings = await _settingsManager.LoadSettingsAsync();

            Apache = settings.Apache;
            MySQL = settings.MySQL;
            Php = settings.PHP;
            Node = settings.Node;
            Redis = settings.Redis;
            PostgreSQL = settings.PostgreSQL;
            Nginx = settings.Nginx;
            MongoDB = settings.MongoDB;
        }

        protected override async Task OnUnloadedAsync()
        {
            await base.OnUnloadedAsync();
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

            var selectedVersion = GetSelectedVersionForService(serviceType);
            if (string.IsNullOrEmpty(selectedVersion))
                return;

            await InstallServiceAsync(serviceType, selectedVersion);
        }

        private string? GetSelectedVersionForService(ServiceType serviceType)
        {
            return serviceType switch
            {
                ServiceType.Apache => SelectedApacheVersion,
                ServiceType.Nginx => SelectedNginxVersion,
                ServiceType.PHP => SelectedPhpVersion,
                ServiceType.MySQL => SelectedMySQLVersion,
                ServiceType.PostgreSQL => SelectedPostgreSQLVersion,
                ServiceType.MongoDB => SelectedMongoDBVersion,
                ServiceType.Node => SelectedNodeVersion,
                ServiceType.Redis => SelectedRedisVersion,
                _ => null
            };
        }

        private async Task InstallServiceAsync(ServiceType serviceType, string version)
        {
            if (version == null)
            {
                return;
            }

            ServiceDefinition? selectedService = null;

            switch (serviceType)
            {
                case ServiceType.Apache:
                    selectedService = Apache?.InstallableVersions.FirstOrDefault(s => s.Name == version);
                    break;
                case ServiceType.Nginx:
                    selectedService = Nginx?.InstallableVersions.FirstOrDefault(s => s.Name == version);
                    break;
                case ServiceType.PHP:
                    selectedService = Php?.InstallableVersions.FirstOrDefault(s => s.Name == version);
                    break;
                case ServiceType.MySQL:
                    selectedService = MySQL?.InstallableVersions.FirstOrDefault(s => s.Name == version);
                    break;
                case ServiceType.PostgreSQL:
                    selectedService = PostgreSQL?.InstallableVersions.FirstOrDefault(s => s.Name == version);
                    break;
                case ServiceType.MongoDB:
                    selectedService = MongoDB?.InstallableVersions.FirstOrDefault(s => s.Name == version);
                    break;
                case ServiceType.Node:
                    selectedService = Node?.InstallableVersions.FirstOrDefault(s => s.Name == version);
                    break;
                case ServiceType.Redis:
                    selectedService = Redis?.InstallableVersions.FirstOrDefault(s => s.Name == version);
                    break;
            }

            if (selectedService == null)
            {
                SetInstallationStatus(serviceType, $"Service definition not found for {serviceType} version {version}");
                return;
            }

            SetInstallationProgress(serviceType, true, true, $"Installing {serviceType} {version}...");

            try
            {
                var progress = new Progress<string>(message =>
                {
                    SetInstallationStatus(serviceType, message);
                });

                // Download the archive to a temp file
                string downloadUrl = selectedService.Url;
                string archivePath = await DownloadHelper.DownloadToTempAsync(downloadUrl, progress);

                // Extract the archive
                string extractPath = Path.Combine(PathHelper.BinPath, serviceType.ToString(), version);
                await ArchiveHelper.ExtractAsync(archivePath, extractPath, selectedService.HasAdditionalDir, progress);

                SetInstallationStatus(serviceType, $"{serviceType} {version} installed successfully.");
                UpdateVersionCollections(serviceType, selectedService);
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

        private void SetInstallationProgress(ServiceType serviceType, bool? isInstalling, bool? showPanel, string? status)
        {
            switch (serviceType)
            {
                case ServiceType.Apache:
                    if (isInstalling.HasValue) IsInstallingApache = isInstalling.Value;
                    if (showPanel.HasValue) ShowApacheInstallationPanel = showPanel.Value;
                    if (status != null) ApacheInstallationStatus = status;
                    break;
                case ServiceType.Nginx:
                    if (isInstalling.HasValue) IsInstallingNginx = isInstalling.Value;
                    if (showPanel.HasValue) ShowNginxInstallationPanel = showPanel.Value;
                    if (status != null) NginxInstallationStatus = status;
                    break;
                case ServiceType.PHP:
                    if (isInstalling.HasValue) IsInstallingPhp = isInstalling.Value;
                    if (showPanel.HasValue) ShowPhpInstallationPanel = showPanel.Value;
                    if (status != null) PhpInstallationStatus = status;
                    break;
                case ServiceType.MySQL:
                    if (isInstalling.HasValue) IsInstallingMysql = isInstalling.Value;
                    if (showPanel.HasValue) ShowMysqlInstallationPanel = showPanel.Value;
                    if (status != null) MysqlInstallationStatus = status;
                    break;
                case ServiceType.PostgreSQL:
                    if (isInstalling.HasValue) IsInstallingPostgreSQL = isInstalling.Value;
                    if (showPanel.HasValue) ShowPostgreSQLInstallationPanel = showPanel.Value;
                    if (status != null) PostgreSQLInstallationStatus = status;
                    break;
                case ServiceType.MongoDB:
                    if (isInstalling.HasValue) IsInstallingMongoDB = isInstalling.Value;
                    if (showPanel.HasValue) ShowMongoDBInstallationPanel = showPanel.Value;
                    if (status != null) MongoDBInstallationStatus = status;
                    break;
                case ServiceType.Node:
                    if (isInstalling.HasValue) IsInstallingNode = isInstalling.Value;
                    if (showPanel.HasValue) ShowNodeInstallationPanel = showPanel.Value;
                    if (status != null) NodeInstallationStatus = status;
                    break;
                case ServiceType.Redis:
                    if (isInstalling.HasValue) IsInstallingRedis = isInstalling.Value;
                    if (showPanel.HasValue) ShowRedisInstallationPanel = showPanel.Value;
                    if (status != null) RedisInstallationStatus = status;
                    break;
            }
        }

        private void SetInstallationStatus(ServiceType serviceType, string status)
        {
            SetInstallationProgress(serviceType, null, null, status);
        }

        private void UpdateVersionCollections(ServiceType serviceType, ServiceDefinition installedService)
        {
            switch (serviceType)
            {
                case ServiceType.Apache:
                    Apache?.InstallableVersions.Remove(installedService);
                    if (!Apache?.AvailableVersions.Contains(installedService.Name) == true)
                        Apache?.AvailableVersions.Add(installedService.Name);
                    break;
                case ServiceType.Nginx:
                    Nginx?.InstallableVersions.Remove(installedService);
                    if (!Nginx?.AvailableVersions.Contains(installedService.Name) == true)
                        Nginx?.AvailableVersions.Add(installedService.Name);
                    break;
                case ServiceType.PHP:
                    Php?.InstallableVersions.Remove(installedService);
                    if (!Php?.AvailableVersions.Contains(installedService.Name) == true)
                        Php?.AvailableVersions.Add(installedService.Name);
                    break;
                case ServiceType.MySQL:
                    MySQL?.InstallableVersions.Remove(installedService);
                    if (!MySQL?.AvailableVersions.Contains(installedService.Name) == true)
                        MySQL?.AvailableVersions.Add(installedService.Name);
                    break;
                case ServiceType.PostgreSQL:
                    PostgreSQL?.InstallableVersions.Remove(installedService);
                    if (!PostgreSQL?.AvailableVersions.Contains(installedService.Name) == true)
                        PostgreSQL?.AvailableVersions.Add(installedService.Name);
                    break;
                case ServiceType.MongoDB:
                    MongoDB?.InstallableVersions.Remove(installedService);
                    if (!MongoDB?.AvailableVersions.Contains(installedService.Name) == true)
                        MongoDB?.AvailableVersions.Add(installedService.Name);
                    break;
                case ServiceType.Node:
                    Node?.InstallableVersions.Remove(installedService);
                    if (!Node?.AvailableVersions.Contains(installedService.Name) == true)
                        Node?.AvailableVersions.Add(installedService.Name);
                    break;
                case ServiceType.Redis:
                    Redis?.InstallableVersions.Remove(installedService);
                    if (!Redis?.AvailableVersions.Contains(installedService.Name) == true)
                        Redis?.AvailableVersions.Add(installedService.Name);
                    break;
            }
        }

    }
}
