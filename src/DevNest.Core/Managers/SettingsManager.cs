using DevNest.Core.Files;
using DevNest.Core.Models;
using DevNest.Core.Services;
using IniParser.Model;
using IniParser.Parser;
using System.ComponentModel;

namespace DevNest.Core
{
    public class SettingsManager
    {

        private readonly FileSystemManager _fileSystemManager;
        private readonly PathManager _pathManager;
        private readonly LogManager _logManager;
        private readonly SettingsFactory _settingsFactory;

        private SettingsModel? _cachedSettings;
        public SettingsModel? CurrentSettings => _cachedSettings;

        private bool _isInitializing = true;


        public SettingsManager(FileSystemManager fileSystemManager, PathManager pathManager, LogManager logManager, SettingsFactory settingsFactory)
        {
            _fileSystemManager = fileSystemManager;
            _pathManager = pathManager;
            _logManager = logManager;
            _settingsFactory = settingsFactory;
        }

        public async Task<SettingsModel> LoadSettingsAsync(bool useCache = true)
        {
            if (useCache && _cachedSettings != null)
            {
                return _cachedSettings;
            }

            _isInitializing = true;

            try
            {

                var settingsFilePath = Path.Combine(_pathManager.ConfigPath, "settings.ini");

                if (await _fileSystemManager.FileExistsAsync(settingsFilePath))
                {
                    var content = await _fileSystemManager.ReadAllTextAsync(settingsFilePath);
                    var iniData = new IniDataParser().Parse(content);
                    var settings = ParseIniToSettings(iniData);

                    await LoadVersionsForSettings(settings);

                    _cachedSettings = settings;
                    SetupAutoSave(settings);

                    return settings;
                }
                else
                {
                    _logManager.Log($"{settingsFilePath} doesnt exist.");
                }
            }
            catch (Exception ex)
            {
                _logManager.Log($"Failed to load settings: {ex.Message}");
            }
            finally
            {
                _isInitializing = false;
            }

            var defaultSettings = await GetDefaultSettingsAsync();
            await LoadVersionsForSettings(defaultSettings);

            _cachedSettings = defaultSettings;
            SetupAutoSave(defaultSettings);

            await SaveSettingsInternalAsync(defaultSettings);

            return defaultSettings;
        }

        private void SetupAutoSave(SettingsModel settings)
        {
            if (_cachedSettings != null)
            {
                _cachedSettings.PropertyChanged -= OnSettingsPropertyChanged;
                _cachedSettings.Apache.PropertyChanged -= OnNestedSettingsPropertyChanged;
                _cachedSettings.MySQL.PropertyChanged -= OnNestedSettingsPropertyChanged;
                _cachedSettings.PHP.PropertyChanged -= OnNestedSettingsPropertyChanged;
                _cachedSettings.Node.PropertyChanged -= OnNestedSettingsPropertyChanged;
                _cachedSettings.Redis.PropertyChanged -= OnNestedSettingsPropertyChanged;
                _cachedSettings.PostgreSQL.PropertyChanged -= OnNestedSettingsPropertyChanged;
                _cachedSettings.Nginx.PropertyChanged -= OnNestedSettingsPropertyChanged;
                _cachedSettings.MongoDB.PropertyChanged -= OnNestedSettingsPropertyChanged;
            }

            settings.PropertyChanged += OnSettingsPropertyChanged;

            settings.Apache.PropertyChanged += OnNestedSettingsPropertyChanged;
            settings.MySQL.PropertyChanged += OnNestedSettingsPropertyChanged;
            settings.PHP.PropertyChanged += OnNestedSettingsPropertyChanged;
            settings.Node.PropertyChanged += OnNestedSettingsPropertyChanged;
            settings.Redis.PropertyChanged += OnNestedSettingsPropertyChanged;
            settings.PostgreSQL.PropertyChanged += OnNestedSettingsPropertyChanged;
            settings.Nginx.PropertyChanged += OnNestedSettingsPropertyChanged;
            settings.MongoDB.PropertyChanged += OnNestedSettingsPropertyChanged;
        }

        private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!_isInitializing && _cachedSettings != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await SaveSettingsInternalAsync(_cachedSettings);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Auto-save failed: {ex.Message}");
                    }
                });
            }
        }

        private void OnNestedSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnSettingsPropertyChanged(sender, e);
        }

        public async Task SaveSettingsAsync(SettingsModel settings)
        {
            if (_cachedSettings != settings)
            {
                _cachedSettings = settings;
                SetupAutoSave(settings);
            }
            await SaveSettingsInternalAsync(settings);
        }

        private async Task SaveSettingsInternalAsync(SettingsModel settings)
        {
            try
            {
                var configPath = _pathManager.ConfigPath;
                var settingsFilePath = Path.Combine(configPath, "settings.ini");
                if (string.IsNullOrEmpty(settingsFilePath))
                {
                    _logManager?.Log($"Saving settings is failed: {settingsFilePath}");
                    return;
                }

                var iniData = ConvertSettingsToIni(settings);
                var content = iniData.ToString();

                await _fileSystemManager.WriteAllTextAsync(settingsFilePath, content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
                throw;
            }
        }

        public Task<SettingsModel> GetDefaultSettingsAsync()
        {
            var settings = new SettingsModel
            {
                StartWithWindows = false,
                MinimizeToSystemTray = false,
                AutoVirtualHosts = true,
                AutoCreateDatabase = false,
            };

            return Task.FromResult(settings);
        }

        private async Task LoadVersionsForSettings(SettingsModel settings)
        {
            await Task.Run(() =>
            {
                foreach (var serviceType in Enum.GetValues(typeof(Enums.ServiceType)).Cast<Enums.ServiceType>())
                {
                    try
                    {
                        var dirName = serviceType.ToString();
                        var servicePath = Path.Combine(_pathManager.BinPath, dirName);
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

            foreach (var serviceProvider in _settingsFactory.GetAllServiceSettingsProviders())
            {
                serviceProvider.ParseFromIni(iniData, settings);
            }

            return settings;
        }

        private IniData ConvertSettingsToIni(SettingsModel settings)
        {
            var iniData = new IniData();

            iniData.Sections.AddSection("General");
            var generalSection = iniData.Sections["General"];
            generalSection.AddKey("StartWithWindows", settings.StartWithWindows.ToString().ToLower());
            generalSection.AddKey("MinimizeToSystemTray", settings.MinimizeToSystemTray.ToString().ToLower());
            generalSection.AddKey("AutoVirtualHosts", settings.AutoVirtualHosts.ToString().ToLower());
            generalSection.AddKey("AutoCreateDatabase", settings.AutoCreateDatabase.ToString().ToLower());

            foreach (var serviceProvider in _settingsFactory.GetAllServiceSettingsProviders())
            {
                serviceProvider.SaveToIni(iniData, settings);
            }

            return iniData;
        }
    }
}
