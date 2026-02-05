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

        public async Task PopulateServiceVersionsAsync(SettingsModel settings, IEnumerable<ServiceModel> installedServices, IEnumerable<ServiceDefinition> availableServices)
        {
            if (settings == null) return;

            ClearServiceVersionCollections(settings);

            var sortedInstalled = installedServices.OrderByDescending(s => s.Name).ToList();
            var sortedAvailable = availableServices.OrderByDescending(s => s.Name).ToList();

            foreach (var service in sortedInstalled)
            {
                var targetCollection = GetServiceSettingsCollection(settings, service.ServiceType);
                if (targetCollection != null && !targetCollection.AvailableVersions.Any(x => x.Name == service.Name))
                {
                    targetCollection.AvailableVersions.Add(service);
                }
            }

            foreach (ServiceType serviceType in Enum.GetValues(typeof(ServiceType)))
            {
                var targetCollection = GetServiceSettingsCollection(settings, serviceType);
                if (targetCollection != null && !string.IsNullOrEmpty(targetCollection.Version))
                {
                    foreach (var service in targetCollection.AvailableVersions)
                    {
                        service.IsSelected = service.Name == targetCollection.Version;
                    }
                }
            }

            foreach (ServiceType serviceType in Enum.GetValues(typeof(ServiceType)))
            {
                var targetCollection = GetServiceSettingsCollection(settings, serviceType);
                if (targetCollection != null && targetCollection.AvailableVersions.Any() && !targetCollection.AvailableVersions.Any(x => x.IsSelected))
                {
                    var firstService = targetCollection.AvailableVersions.First();
                    firstService.IsSelected = true;
                    targetCollection.Version = firstService.Name;
                }
            }

            foreach (var serviceDefinition in sortedAvailable)
            {
                var targetCollection = GetServiceSettingsCollection(settings, serviceDefinition.ServiceType);
                if (targetCollection != null && !targetCollection.AvailableVersions.Any(x => x.Name == serviceDefinition.Name))
                {
                    targetCollection.InstallableVersions.Add(serviceDefinition);
                }
            }

            await Task.CompletedTask;
        }

        public async Task PopulateCommandsAsync(SettingsModel settings)
        {
            if (settings == null) return;
            if (_platformSerciceFacory == null)
            {
                throw new InvalidOperationException("PlatformServiceFactory must be set before calling this method.");
            }

            var _commandManager = _platformSerciceFacory.GetCommandManager(settings);

            foreach (ServiceType serviceType in Enum.GetValues(typeof(ServiceType)))
            {
                var serviceSettings = GetServiceSettingsCollection(settings, serviceType);
                if (serviceSettings != null)
                {
                    foreach (ServiceModel service in serviceSettings.AvailableVersions)
                    {
                        var (command, workingDirectory) = await _commandManager.GetCommand(service, settings);
                        service.Command = command;
                        service.WorkingDirectory = workingDirectory;
                    }
                }
            }
        }

        public void SetSelectedVersion(SettingsModel settings, IEnumerable<ServiceModel> installedServices)
        {
            if (settings == null) return;

            foreach (var service in installedServices)
            {
                if (service.IsSelected)
                {
                    var targetCollection = GetServiceSettingsCollection(settings, service.ServiceType);
                    if (targetCollection != null)
                    {
                        targetCollection.Version = service.Name;
                    }
                }
            }

        }

        private void ClearServiceVersionCollections(SettingsModel settings)
        {
            if (settings == null) return;

            settings.Apache.AvailableVersions.Clear();
            settings.Apache.InstallableVersions.Clear();
            settings.MySQL.AvailableVersions.Clear();
            settings.MySQL.InstallableVersions.Clear();
            settings.PHP.AvailableVersions.Clear();
            settings.PHP.InstallableVersions.Clear();
            settings.Nginx.AvailableVersions.Clear();
            settings.Nginx.InstallableVersions.Clear();
            settings.Node.AvailableVersions.Clear();
            settings.Node.InstallableVersions.Clear();
            settings.Redis.AvailableVersions.Clear();
            settings.Redis.InstallableVersions.Clear();
            settings.PostgreSQL.AvailableVersions.Clear();
            settings.PostgreSQL.InstallableVersions.Clear();
            settings.MongoDB.AvailableVersions.Clear();
            settings.MongoDB.InstallableVersions.Clear();
        }

        private ServiceSettingsModel? GetServiceSettingsCollection(SettingsModel settings, ServiceType serviceType)
        {
            if (settings == null) return null;

            return serviceType switch
            {
                ServiceType.Apache => settings.Apache,
                ServiceType.MySQL => settings.MySQL,
                ServiceType.PHP => settings.PHP,
                ServiceType.Nginx => settings.Nginx,
                ServiceType.Node => settings.Node,
                ServiceType.Redis => settings.Redis,
                ServiceType.PostgreSQL => settings.PostgreSQL,
                ServiceType.MongoDB => settings.MongoDB,
                _ => null
            };
        }

    }
}
