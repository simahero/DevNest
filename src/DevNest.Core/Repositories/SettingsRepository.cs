using DevNest.Core.Enums;
using DevNest.Core.Helpers;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using DevNest.Core.Services;
using IniParser.Model;
using IniParser.Parser;

namespace DevNest.Core.Repositories
{

    public class SettingsRepository : ISettingsRepository
    {
        private PlatformServiceFactory? _platformSerciceFacory;
        private readonly SettingsFactory _settingsFactory;

        public SettingsModel? Settings { get; private set; }

        public SettingsRepository(SettingsFactory settingsFactory)
        {
            _settingsFactory = settingsFactory;
        }

        public void SetPlatformServiceFactory(PlatformServiceFactory platformServiceFactory)
        {
            _platformSerciceFacory = platformServiceFactory;
        }

        public async Task<SettingsModel> GetSettingsAsync()
        {
            Settings = await LoadSettingsAsync();
            return Settings;
        }

        private async Task<SettingsModel> LoadSettingsAsync()
        {
            try
            {
                var baseSettingsPath = PathHelper.BaseSettingsPath;
                var settings = new SettingsModel
                {
                    StartWithWindows = false,
                    MinimizeToSystemTray = false,
                    AutoVirtualHosts = true,
                    AutoCreateDatabase = false,
                    NgrokDomain = string.Empty,
                    NgrokApiKey = string.Empty,
                    UseWSL = false,
                };

                if (await FileSystemHelper.FileExistsAsync(baseSettingsPath))
                {
                    var baseContent = await FileSystemHelper.ReadFileWithRetryAsync(baseSettingsPath);
                    var baseIniData = new IniDataParser().Parse(baseContent);
                    settings = ParseIniToSettings(baseIniData);
                }
                else
                {
                    _ = Logger.Log($"{baseSettingsPath} doesn't exist, using defaults.");
                }

                PathHelper.SetUseWSL(settings.UseWSL);

                var platformSettingsPath = PathHelper.SettingsPath;

                if (await FileSystemHelper.FileExistsAsync(platformSettingsPath))
                {
                    var platformContent = await FileSystemHelper.ReadFileWithRetryAsync(platformSettingsPath);
                    var platformIniData = new IniDataParser().Parse(platformContent);

                    foreach (var serviceProvider in _settingsFactory.GetAllServiceSettingsProviders())
                    {
                        serviceProvider.ParseFromIni(platformIniData, settings);
                    }
                }
                else
                {
                    _ = Logger.Log($"{platformSettingsPath} doesn't exist.");
                }

                return settings;
            }
            catch (Exception ex)
            {
                _ = Logger.Log($"Failed to load settings: {ex.Message}");

                var defaultSettings = new SettingsModel
                {
                    StartWithWindows = false,
                    MinimizeToSystemTray = false,
                    AutoVirtualHosts = true,
                    AutoCreateDatabase = false,
                    NgrokDomain = string.Empty,
                    NgrokApiKey = string.Empty,
                    UseWSL = false,
                };
                return defaultSettings;
            }
        }

        private SettingsModel ParseIniToSettings(IniData iniData)
        {
            var settings = new SettingsModel();

            if (iniData.Sections.ContainsSection("General"))
            {
                var generalSection = iniData.Sections["General"];
                settings.StartWithWindows = bool.Parse(generalSection["StartWithWindows"] ?? "false");
                settings.MinimizeToSystemTray = bool.Parse(generalSection["MinimizeToSystemTray"] ?? "false");
                settings.AutoVirtualHosts = bool.Parse(generalSection["AutoVirtualHosts"] ?? "true");
                settings.AutoCreateDatabase = bool.Parse(generalSection["AutoCreateDatabase"] ?? "false");
            }
            if (iniData.Sections.ContainsSection("Ngrok"))
            {
                var ngrokSection = iniData.Sections["Ngrok"];
                settings.NgrokDomain = ngrokSection["Domain"] ?? string.Empty;
                settings.NgrokApiKey = ngrokSection["ApiKey"] ?? string.Empty;
            }
            if (iniData.Sections.ContainsSection("WSL"))
            {
                var wslSection = iniData.Sections["WSL"];
                settings.UseWSL = bool.Parse(wslSection["UseWSL"] ?? "false");
            }

            foreach (var serviceProvider in _settingsFactory.GetAllServiceSettingsProviders())
            {
                serviceProvider.ParseFromIni(iniData, settings);
            }

            return settings;
        }

        public async Task PopulateServiceVersionsAsync(IEnumerable<ServiceModel> installedServices, IEnumerable<ServiceDefinition> availableServices)
        {
            if (Settings == null) return;

            ClearServiceVersionCollections();

            var sortedInstalled = installedServices.OrderByDescending(s => s.Name).ToList();
            var sortedAvailable = availableServices.OrderByDescending(s => s.Name).ToList();

            foreach (var service in sortedInstalled)
            {
                var targetCollection = GetServiceSettingsCollection(service.ServiceType);
                if (targetCollection != null && !targetCollection.AvailableVersions.Any(x => x.Name == service.Name))
                {
                    service.IsSelected = service.Name == targetCollection.Version;
                    targetCollection.AvailableVersions.Add(service);
                }
            }

            foreach (var serviceDefinition in sortedAvailable)
            {
                var targetCollection = GetServiceSettingsCollection(serviceDefinition.ServiceType);
                if (targetCollection != null && !targetCollection.AvailableVersions.Any(x => x.Name == serviceDefinition.Name))
                {
                    targetCollection.InstallableVersions.Add(serviceDefinition);
                }
            }

            await Task.CompletedTask;
        }

        public async Task PopulateCommandsAsync()
        {
            if (Settings == null) return;
            if (_platformSerciceFacory == null)
            {
                throw new InvalidOperationException("PlatformServiceFactory must be set before calling this method.");
            }

            var _commandManager = _platformSerciceFacory.GetCommandManager();

            foreach (ServiceType serviceType in Enum.GetValues(typeof(ServiceType)))
            {
                var serviceSettings = GetServiceSettingsCollection(serviceType);
                if (serviceSettings != null)
                {
                    foreach (ServiceModel service in serviceSettings.AvailableVersions)
                    {
                        var (command, workingDirectory) = await _commandManager.GetCommand(service, Settings);
                        service.Command = command;
                        service.WorkingDirectory = workingDirectory;
                    }
                }
            }
        }

        public void SetSelectedVersion(IEnumerable<ServiceModel> installedServices)
        {
            if (Settings == null) return;

            foreach (var service in installedServices)
            {
                if (service.IsSelected)
                {
                    var targetCollection = GetServiceSettingsCollection(service.ServiceType);
                    if (targetCollection != null)
                    {
                        targetCollection.Version = service.Name;
                    }
                }
            }

        }

        private void ClearServiceVersionCollections()
        {
            if (Settings == null) return;

            Settings.Apache.AvailableVersions.Clear();
            Settings.Apache.InstallableVersions.Clear();
            Settings.MySQL.AvailableVersions.Clear();
            Settings.MySQL.InstallableVersions.Clear();
            Settings.PHP.AvailableVersions.Clear();
            Settings.PHP.InstallableVersions.Clear();
            Settings.Nginx.AvailableVersions.Clear();
            Settings.Nginx.InstallableVersions.Clear();
            Settings.Node.AvailableVersions.Clear();
            Settings.Node.InstallableVersions.Clear();
            Settings.Redis.AvailableVersions.Clear();
            Settings.Redis.InstallableVersions.Clear();
            Settings.PostgreSQL.AvailableVersions.Clear();
            Settings.PostgreSQL.InstallableVersions.Clear();
            Settings.MongoDB.AvailableVersions.Clear();
            Settings.MongoDB.InstallableVersions.Clear();
        }

        private ServiceSettingsModel? GetServiceSettingsCollection(ServiceType serviceType)
        {
            if (Settings == null) return null;

            return serviceType switch
            {
                ServiceType.Apache => Settings.Apache,
                ServiceType.MySQL => Settings.MySQL,
                ServiceType.PHP => Settings.PHP,
                ServiceType.Nginx => Settings.Nginx,
                ServiceType.Node => Settings.Node,
                ServiceType.Redis => Settings.Redis,
                ServiceType.PostgreSQL => Settings.PostgreSQL,
                ServiceType.MongoDB => Settings.MongoDB,
                _ => null
            };
        }

    }
}
