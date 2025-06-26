using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using DevNest.Services.Settings;
using IniParser.Model;
using IniParser.Parser;
using System.ComponentModel;

namespace DevNest.Services
{
    public class SettingsManager
    {

        private readonly IFileSystemService _fileSystemService;
        private readonly IPathService _pathService;
        private readonly LogManager _logManager;
        private readonly SettingsFactory _settingsFactory;

        private SettingsModel? _cachedSettings;
        public SettingsModel? CurrentSettings => _cachedSettings;

        private bool _isInitializing = true;
        private bool _autoSaveEnabled = true;


        public SettingsManager(IFileSystemService fileSystemService, IPathService pathService, LogManager logManager, SettingsFactory settingsFactory)
        {
            _fileSystemService = fileSystemService;
            _pathService = pathService;
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

                var settingsFilePath = Path.Combine(_pathService.ConfigPath, "settings.ini");

                if (await _fileSystemService.FileExistsAsync(settingsFilePath))
                {
                    var content = await _fileSystemService.ReadAllTextAsync(settingsFilePath);
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
            }

            settings.PropertyChanged += OnSettingsPropertyChanged;

            settings.Apache.PropertyChanged += OnNestedSettingsPropertyChanged;
            settings.MySQL.PropertyChanged += OnNestedSettingsPropertyChanged;
            settings.PHP.PropertyChanged += OnNestedSettingsPropertyChanged;
            settings.Node.PropertyChanged += OnNestedSettingsPropertyChanged;
            settings.Redis.PropertyChanged += OnNestedSettingsPropertyChanged;
            settings.PostgreSQL.PropertyChanged += OnNestedSettingsPropertyChanged;
            settings.Nginx.PropertyChanged += OnNestedSettingsPropertyChanged;
        }

        private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_autoSaveEnabled && !_isInitializing && _cachedSettings != null)
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

        public void SetAutoSaveEnabled(bool enabled)
        {
            _autoSaveEnabled = enabled;
        }

        private async Task SaveSettingsInternalAsync(SettingsModel settings)
        {
            try
            {
                var configPath = _pathService.ConfigPath;
                var settingsFilePath = Path.Combine(configPath, "settings.ini");
                if (string.IsNullOrEmpty(settingsFilePath))
                {
                    _logManager?.Log($"Saving settings is failed: {settingsFilePath}");
                    return;
                }

                var iniData = ConvertSettingsToIni(settings);
                var content = iniData.ToString();

                await _fileSystemService.WriteAllTextAsync(settingsFilePath, content);
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
                try
                {
                    var apachePath = Path.Combine(_pathService.BinPath, "Apache");
                    if (Directory.Exists(apachePath))
                    {
                        var versionDirectories = Directory.GetDirectories(apachePath)
                            .Select(dir => Path.GetFileName(dir))
                            .Where(dir => !string.IsNullOrEmpty(dir))
                            .OrderBy(version => version)
                            .ToList();

                        settings.Apache.AvailableVersions.Clear();
                        foreach (var version in versionDirectories)
                        {
                            settings.Apache.AvailableVersions.Add(version);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading Apache versions: {ex.Message}");
                }

                // Load MySQL versions
                try
                {
                    var mysqlPath = Path.Combine(_pathService.BinPath, "MySQL");
                    if (Directory.Exists(mysqlPath))
                    {
                        var versionDirectories = Directory.GetDirectories(mysqlPath)
                            .Select(dir => Path.GetFileName(dir))
                            .Where(dir => !string.IsNullOrEmpty(dir))
                            .OrderBy(version => version)
                            .ToList();

                        settings.MySQL.AvailableVersions.Clear();
                        foreach (var version in versionDirectories)
                        {
                            settings.MySQL.AvailableVersions.Add(version);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading MySQL versions: {ex.Message}");
                }

                // Load PHP versions
                try
                {
                    var phpPath = Path.Combine(_pathService.BinPath, "PHP");
                    if (Directory.Exists(phpPath))
                    {
                        var versionDirectories = Directory.GetDirectories(phpPath)
                            .Select(dir => Path.GetFileName(dir))
                            .Where(dir => !string.IsNullOrEmpty(dir))
                            .OrderBy(version => version)
                            .ToList();

                        settings.PHP.AvailableVersions.Clear();
                        foreach (var version in versionDirectories)
                        {
                            settings.PHP.AvailableVersions.Add(version);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading PHP versions: {ex.Message}");
                }

                // Load Node versions
                try
                {
                    var nodePath = Path.Combine(_pathService.BinPath, "Node");
                    if (Directory.Exists(nodePath))
                    {
                        var versionDirectories = Directory.GetDirectories(nodePath)
                            .Select(dir => Path.GetFileName(dir))
                            .Where(dir => !string.IsNullOrEmpty(dir))
                            .OrderBy(version => version)
                            .ToList();

                        settings.Node.AvailableVersions.Clear();
                        foreach (var version in versionDirectories)
                        {
                            settings.Node.AvailableVersions.Add(version);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading Node versions: {ex.Message}");
                }

                // Load Redis versions
                try
                {
                    var redisPath = Path.Combine(_pathService.BinPath, "Redis");
                    if (Directory.Exists(redisPath))
                    {
                        var versionDirectories = Directory.GetDirectories(redisPath)
                            .Select(dir => Path.GetFileName(dir))
                            .Where(dir => !string.IsNullOrEmpty(dir))
                            .OrderBy(version => version)
                            .ToList();

                        settings.Redis.AvailableVersions.Clear();
                        foreach (var version in versionDirectories)
                        {
                            settings.Redis.AvailableVersions.Add(version);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading Redis versions: {ex.Message}");
                }

                // Load PostgreSQL versions
                try
                {
                    var postgresqlPath = Path.Combine(_pathService.BinPath, "PostgreSQL");
                    if (Directory.Exists(postgresqlPath))
                    {
                        var versionDirectories = Directory.GetDirectories(postgresqlPath)
                            .Select(dir => Path.GetFileName(dir))
                            .Where(dir => !string.IsNullOrEmpty(dir))
                            .OrderBy(version => version)
                            .ToList();

                        settings.PostgreSQL.AvailableVersions.Clear();
                        foreach (var version in versionDirectories)
                        {
                            settings.PostgreSQL.AvailableVersions.Add(version);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading PostgreSQL versions: {ex.Message}");
                }

                // Load Nginx versions
                try
                {
                    var nginxPath = Path.Combine(_pathService.BinPath, "Nginx");
                    if (Directory.Exists(nginxPath))
                    {
                        var versionDirectories = Directory.GetDirectories(nginxPath)
                            .Select(dir => Path.GetFileName(dir))
                            .Where(dir => !string.IsNullOrEmpty(dir))
                            .OrderBy(version => version)
                            .ToList();

                        settings.Nginx.AvailableVersions.Clear();
                        foreach (var version in versionDirectories)
                        {
                            settings.Nginx.AvailableVersions.Add(version);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading Nginx versions: {ex.Message}");
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
