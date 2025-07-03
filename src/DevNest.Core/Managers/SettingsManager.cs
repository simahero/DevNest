using DevNest.Core.Helpers;
using DevNest.Core.Models;
using DevNest.Core.Services;
using IniParser.Model;
using IniParser.Parser;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;

namespace DevNest.Core
{
    public class SettingsManager
    {

        private readonly SettingsFactory _settingsFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly SemaphoreSlim _fileLock = new(1, 1);


        private Model? _cachedSettings;
        public Model? CurrentSettings => _cachedSettings;

        private bool _isInitializing = true;

        private CancellationTokenSource? _autoSaveCts;

        public SettingsManager(SettingsFactory settingsFactory, IServiceProvider serviceProvider)
        {
            _settingsFactory = settingsFactory;
            _serviceProvider = serviceProvider;
        }

        public async Task<Model> LoadSettingsAsync(bool useCache = true)
        {
            if (useCache && _cachedSettings != null)
            {
                return _cachedSettings;
            }

            await _fileLock.WaitAsync();

            try
            {

                _isInitializing = true;
                var settingsFilePath = PathHelper.SettingsPath;

                if (await FileSystemHelper.FileExistsAsync(settingsFilePath))
                {
                    var content = await FileSystemHelper.ReadFileWithRetryAsync(settingsFilePath);
                    var iniData = new IniDataParser().Parse(content);
                    var settings = ParseIniToSettings(iniData);

                    await LoadVersionsForSettings(settings);

                    _cachedSettings = settings;
                    _isInitializing = false;
                    SetupAutoSave(_cachedSettings);

                    return settings;
                }
                else
                {
                    _ = Logger.Log($"{settingsFilePath} doesnt exist.");
                }
            }
            catch (Exception ex)
            {
                _ = Logger.Log($"Failed to load settings: {ex.Message}");
            }
            finally
            {
                _fileLock.Release();
            }

            var defaultSettings = new Model
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

            _cachedSettings = defaultSettings;
            _isInitializing = false;
            SetupAutoSave(_cachedSettings);

            await SaveSettingsInternalAsync(defaultSettings);

            return defaultSettings;
        }

        private void SetupAutoSave(Model settings)
        {
            if (_cachedSettings != null)
            {
                _cachedSettings.PropertyChanged -= OnSettingsPropertyChanged;
                UnsubscribeNested(_cachedSettings);
            }

            settings.PropertyChanged += OnSettingsPropertyChanged;
            SubscribeNested(settings);
            SubscribeToNestedReplacement(settings);
        }

        private void SubscribeNested(Model settings)
        {
            if (settings.Apache != null) settings.Apache.PropertyChanged += OnNestedSettingsPropertyChanged;
            if (settings.MySQL != null) settings.MySQL.PropertyChanged += OnNestedSettingsPropertyChanged;
            if (settings.PHP != null) settings.PHP.PropertyChanged += OnNestedSettingsPropertyChanged;
            if (settings.Node != null) settings.Node.PropertyChanged += OnNestedSettingsPropertyChanged;
            if (settings.Redis != null) settings.Redis.PropertyChanged += OnNestedSettingsPropertyChanged;
            if (settings.PostgreSQL != null) settings.PostgreSQL.PropertyChanged += OnNestedSettingsPropertyChanged;
            if (settings.Nginx != null) settings.Nginx.PropertyChanged += OnNestedSettingsPropertyChanged;
            if (settings.MongoDB != null) settings.MongoDB.PropertyChanged += OnNestedSettingsPropertyChanged;
        }

        private void UnsubscribeNested(Model settings)
        {
            if (settings.Apache != null) settings.Apache.PropertyChanged -= OnNestedSettingsPropertyChanged;
            if (settings.MySQL != null) settings.MySQL.PropertyChanged -= OnNestedSettingsPropertyChanged;
            if (settings.PHP != null) settings.PHP.PropertyChanged -= OnNestedSettingsPropertyChanged;
            if (settings.Node != null) settings.Node.PropertyChanged -= OnNestedSettingsPropertyChanged;
            if (settings.Redis != null) settings.Redis.PropertyChanged -= OnNestedSettingsPropertyChanged;
            if (settings.PostgreSQL != null) settings.PostgreSQL.PropertyChanged -= OnNestedSettingsPropertyChanged;
            if (settings.Nginx != null) settings.Nginx.PropertyChanged -= OnNestedSettingsPropertyChanged;
            if (settings.MongoDB != null) settings.MongoDB.PropertyChanged -= OnNestedSettingsPropertyChanged;
        }

        private void SubscribeToNestedReplacement(Model settings)
        {
            settings.PropertyChanged += (s, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(settings.Apache):
                    case nameof(settings.MySQL):
                    case nameof(settings.PHP):
                    case nameof(settings.Node):
                    case nameof(settings.Redis):
                    case nameof(settings.PostgreSQL):
                    case nameof(settings.Nginx):
                    case nameof(settings.MongoDB):
                        UnsubscribeNested(settings);
                        SubscribeNested(settings);
                        break;
                }
            };
        }

        private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!_isInitializing && _cachedSettings != null)
            {
                _autoSaveCts?.Cancel();
                _autoSaveCts = new CancellationTokenSource();
                var token = _autoSaveCts.Token;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(2000, token); // Debounce delay
                        if (!token.IsCancellationRequested)
                        {
                            await SaveSettingsInternalAsync(_cachedSettings);
                        }
                    }
                    catch (TaskCanceledException) { }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Auto-save failed: {ex.Message}");
                        _ = Logger.Log($"Auto-save failed: {ex.Message}");
                    }
                }, token);
            }
        }

        private void OnNestedSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnSettingsPropertyChanged(sender, e);
        }

        private async Task SaveSettingsInternalAsync(Model settings)
        {

            await _fileLock.WaitAsync();

            try
            {
                var configPath = PathHelper.ConfigPath;
                var settingsFilePath = PathHelper.SettingsPath;
                if (string.IsNullOrEmpty(settingsFilePath))
                {
                    _ = Logger.Log($"Saving settings is failed: {settingsFilePath}");
                    return;
                }

                var iniData = ConvertSettingsToIni(settings);
                var content = iniData.ToString();

                await FileSystemHelper.WriteFileWithRetryAsync(settingsFilePath, content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
                throw;
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task LoadVersionsForSettings(Model settings)
        {
            await LoadInstalledVersionsForSettings(settings);
            await LoadInstallableVersionsForSettings(settings);
        }

        private async Task LoadInstalledVersionsForSettings(Model settings)
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

        private async Task LoadInstallableVersionsForSettings(Model settings)
        {
            try
            {
                var serviceManager = _serviceProvider.GetRequiredService<ServiceManager>();
                var availableServices = await serviceManager.GetAvailableServices();

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
        }

        private Model ParseIniToSettings(IniData iniData)
        {
            var settings = new Model();

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

        private IniData ConvertSettingsToIni(Model settings)
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

            foreach (var serviceProvider in _settingsFactory.GetAllServiceSettingsProviders())
            {
                serviceProvider.SaveToIni(iniData, settings);
            }

            return iniData;
        }
    }
}
