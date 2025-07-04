using DevNest.Core.Helpers;
using DevNest.Core.Models;
using DevNest.Core.Services;
using DevNest.Core.State;
using IniParser.Model;
using IniParser.Parser;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;

namespace DevNest.Core
{
    public class SettingsManager
    {

        private readonly AppState _appState;
        private readonly SettingsFactory _settingsFactory;

        public SettingsManager(AppState appState, SettingsFactory settingsFactory)
        {
            _appState = appState;
            _settingsFactory = settingsFactory;
        }

        public async Task<SettingsModel> LoadSettingsAsync()
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
                    UseWLS = false,
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

                PathHelper.SetUseWSL(settings.UseWLS);

                var platformSettingsPath = PathHelper.SettingsPath;

                if (await FileSystemHelper.FileExistsAsync(platformSettingsPath))
                {
                    var platformContent = await FileSystemHelper.ReadFileWithRetryAsync(platformSettingsPath);
                    var platformIniData = new IniDataParser().Parse(platformContent);

                    MergePlatformSettings(settings, platformIniData);
                }
                else
                {
                    _ = Logger.Log($"{platformSettingsPath} doesn't exist.");
                }

                await LoadVersionsForSettings(settings);
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
                    UseWLS = false,
                };
                await LoadVersionsForSettings(defaultSettings);
                return defaultSettings;
            }
        }

        public async Task LoadVersionsForSettings(SettingsModel settings)
        {
            await LoadInstalledVersionsForSettings(settings);
            await LoadInstallableVersionsForSettings(settings);
        }

        private async Task LoadInstalledVersionsForSettings(SettingsModel settings)
        {
            await Task.Run(() =>
            {
                foreach (var serviceType in Enum.GetValues(typeof(Enums.ServiceType)).Cast<Enums.ServiceType>())
                {
                    try
                    {
                        var dirName = serviceType.ToString();
                        var servicePath = Path.Combine(PathHelper.BinPath, dirName);
                        if (Directory.Exists(servicePath))
                        {
                            var versionDirectories = Directory.GetDirectories(servicePath)
                                .Select(dir => Path.GetFileName(dir))
                                .Where(dir => !string.IsNullOrEmpty(dir))
                                .OrderBy(version => version)
                                .ToList();

                            switch (serviceType)
                            {
                                case Enums.ServiceType.Apache:
                                    settings.Apache.AvailableVersions.Clear();
                                    foreach (var version in versionDirectories)
                                        settings.Apache.AvailableVersions.Add(version);
                                    break;
                                case Enums.ServiceType.MySQL:
                                    settings.MySQL.AvailableVersions.Clear();
                                    foreach (var version in versionDirectories)
                                        settings.MySQL.AvailableVersions.Add(version);
                                    break;
                                case Enums.ServiceType.PHP:
                                    settings.PHP.AvailableVersions.Clear();
                                    foreach (var version in versionDirectories)
                                        settings.PHP.AvailableVersions.Add(version);
                                    break;
                                case Enums.ServiceType.Node:
                                    settings.Node.AvailableVersions.Clear();
                                    foreach (var version in versionDirectories)
                                        settings.Node.AvailableVersions.Add(version);
                                    break;
                                case Enums.ServiceType.Redis:
                                    settings.Redis.AvailableVersions.Clear();
                                    foreach (var version in versionDirectories)
                                        settings.Redis.AvailableVersions.Add(version);
                                    break;
                                case Enums.ServiceType.PostgreSQL:
                                    settings.PostgreSQL.AvailableVersions.Clear();
                                    foreach (var version in versionDirectories)
                                        settings.PostgreSQL.AvailableVersions.Add(version);
                                    break;
                                case Enums.ServiceType.Nginx:
                                    settings.Nginx.AvailableVersions.Clear();
                                    foreach (var version in versionDirectories)
                                        settings.Nginx.AvailableVersions.Add(version);
                                    break;
                                case Enums.ServiceType.MongoDB:
                                    settings.MongoDB.AvailableVersions.Clear();
                                    foreach (var version in versionDirectories)
                                        settings.MongoDB.AvailableVersions.Add(version);
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading {serviceType} versions: {ex.Message}");
                    }
                }
            });
        }

        private async Task LoadInstallableVersionsForSettings(SettingsModel settings)
        {
            await Task.Run(() =>
            {
                try
                {
                    var availableServices = _appState.AvailableServices;

                    var servicesByType = availableServices.GroupBy(s => s.ServiceType);

                    foreach (var serviceGroup in servicesByType)
                    {
                        var serviceType = serviceGroup.Key;
                        var serviceDefinitions = serviceGroup.ToList();

                        var installedVersions = serviceType switch
                        {
                            Enums.ServiceType.Apache => settings.Apache.AvailableVersions.ToHashSet(),
                            Enums.ServiceType.MySQL => settings.MySQL.AvailableVersions.ToHashSet(),
                            Enums.ServiceType.PHP => settings.PHP.AvailableVersions.ToHashSet(),
                            Enums.ServiceType.Node => settings.Node.AvailableVersions.ToHashSet(),
                            Enums.ServiceType.Redis => settings.Redis.AvailableVersions.ToHashSet(),
                            Enums.ServiceType.PostgreSQL => settings.PostgreSQL.AvailableVersions.ToHashSet(),
                            Enums.ServiceType.Nginx => settings.Nginx.AvailableVersions.ToHashSet(),
                            Enums.ServiceType.MongoDB => settings.MongoDB.AvailableVersions.ToHashSet(),
                            _ => new HashSet<string>()
                        };

                        var installableServiceDefinitions = serviceDefinitions.Where(s => !installedVersions.Contains(s.Name)).ToList();

                        switch (serviceType)
                        {
                            case Enums.ServiceType.Apache:
                                settings.Apache.InstallableVersions.Clear();
                                foreach (var serviceDefinition in installableServiceDefinitions)
                                    settings.Apache.InstallableVersions.Add(serviceDefinition);
                                break;
                            case Enums.ServiceType.MySQL:
                                settings.MySQL.InstallableVersions.Clear();
                                foreach (var serviceDefinition in installableServiceDefinitions)
                                    settings.MySQL.InstallableVersions.Add(serviceDefinition);
                                break;
                            case Enums.ServiceType.PHP:
                                settings.PHP.InstallableVersions.Clear();
                                foreach (var serviceDefinition in installableServiceDefinitions)
                                    settings.PHP.InstallableVersions.Add(serviceDefinition);
                                break;
                            case Enums.ServiceType.Node:
                                settings.Node.InstallableVersions.Clear();
                                foreach (var serviceDefinition in installableServiceDefinitions)
                                    settings.Node.InstallableVersions.Add(serviceDefinition);
                                break;
                            case Enums.ServiceType.Redis:
                                settings.Redis.InstallableVersions.Clear();
                                foreach (var serviceDefinition in installableServiceDefinitions)
                                    settings.Redis.InstallableVersions.Add(serviceDefinition);
                                break;
                            case Enums.ServiceType.PostgreSQL:
                                settings.PostgreSQL.InstallableVersions.Clear();
                                foreach (var serviceDefinition in installableServiceDefinitions)
                                    settings.PostgreSQL.InstallableVersions.Add(serviceDefinition);
                                break;
                            case Enums.ServiceType.Nginx:
                                settings.Nginx.InstallableVersions.Clear();
                                foreach (var serviceDefinition in installableServiceDefinitions)
                                    settings.Nginx.InstallableVersions.Add(serviceDefinition);
                                break;
                            case Enums.ServiceType.MongoDB:
                                settings.MongoDB.InstallableVersions.Clear();
                                foreach (var serviceDefinition in installableServiceDefinitions)
                                    settings.MongoDB.InstallableVersions.Add(serviceDefinition);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading installable versions: {ex.Message}");
                }
            });
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
                settings.UseWLS = bool.Parse(wslSection["UseWLS"] ?? "false");
            }

            foreach (var serviceProvider in _settingsFactory.GetAllServiceSettingsProviders())
            {
                serviceProvider.ParseFromIni(iniData, settings);
            }

            return settings;
        }

        public IniData ConvertSettingsToIni(SettingsModel settings)
        {
            var iniData = new IniData();

            iniData.Sections.AddSection("General");
            var generalSection = iniData.Sections["General"];
            generalSection.AddKey("StartWithWindows", settings.StartWithWindows.ToString().ToLower());
            generalSection.AddKey("MinimizeToSystemTray", settings.MinimizeToSystemTray.ToString().ToLower());
            generalSection.AddKey("AutoVirtualHosts", settings.AutoVirtualHosts.ToString().ToLower());
            generalSection.AddKey("AutoCreateDatabase", settings.AutoCreateDatabase.ToString().ToLower());

            iniData.Sections.AddSection("Ngrok");
            var ngrokSection = iniData.Sections["Ngrok"];
            ngrokSection.AddKey("Domain", settings.NgrokDomain ?? string.Empty);
            ngrokSection.AddKey("ApiKey", settings.NgrokApiKey ?? string.Empty);

            iniData.Sections.AddSection("WSL");
            var wslSection = iniData.Sections["WSL"];
            wslSection.AddKey("UseWLS", settings.UseWLS.ToString().ToLower());

            return iniData;
        }

        public IniData ConvertPlatformSettingsToIni(SettingsModel settings)
        {
            var iniData = new IniData();

            foreach (var serviceProvider in _settingsFactory.GetAllServiceSettingsProviders())
            {
                serviceProvider.SaveToIni(iniData, settings);
            }

            return iniData;
        }

        public void MergePlatformSettings(SettingsModel settings, IniData platformIniData)
        {
            foreach (var serviceProvider in _settingsFactory.GetAllServiceSettingsProviders())
            {
                serviceProvider.ParseFromIni(platformIniData, settings);
            }
        }

        public void MergePlatformSettingsFromIni(IniData platformIniData, SettingsModel settings)
        {
            MergePlatformSettings(settings, platformIniData);
        }

    }
}
