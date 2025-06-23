using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using IniParser;
using IniParser.Model;
using IniParser.Parser;
using System.Collections.ObjectModel;

namespace DevNest.Services
{
    public class AppSettingsService : IAppSettingsService
    {
        private static readonly string SettingsFilePath = @"C:\DevNest\settings.ini";
        private readonly IFileSystemService _fileSystemService;
        private readonly FileIniDataParser _parser;
        private AppSettings? _cachedSettings;

        public AppSettingsService(IFileSystemService fileSystemService)
        {
            _fileSystemService = fileSystemService;
            _parser = new FileIniDataParser();
        }

        public async Task<AppSettings> LoadSettingsAsync()
        {
            if (_cachedSettings != null)
                return _cachedSettings;

            try
            {
                if (await _fileSystemService.FileExistsAsync(SettingsFilePath))
                {
                    var content = await _fileSystemService.ReadAllTextAsync(SettingsFilePath);
                    var iniData = new IniDataParser().Parse(content);
                    var settings = ParseIniToSettings(iniData);

                    await LoadCategoriesAsync(settings);
                    _cachedSettings = settings;
                    return settings;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
            }

            var defaultSettings = await GetDefaultSettingsAsync();
            _cachedSettings = defaultSettings;
            return defaultSettings;
        }

        public async Task SaveSettingsAsync(AppSettings settings)
        {
            try
            {
                var directory = System.IO.Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(directory) && !await _fileSystemService.DirectoryExistsAsync(directory))
                {
                    await _fileSystemService.CreateDirectoryAsync(directory);
                }

                var iniData = ConvertSettingsToIni(settings);
                var content = iniData.ToString();

                await _fileSystemService.WriteAllTextAsync(SettingsFilePath, content);
                _cachedSettings = settings;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
                throw;
            }
        }
        public async Task<AppSettings> GetDefaultSettingsAsync()
        {
            var settings = new AppSettings
            {
                StartWithWindows = false,
                MinimizeToSystemTray = false,
                AutoVirtualHosts = true,
                AutoCreateDatabase = false,
                InstallDirectory = @"C:\DevNest",
                Versions = new ObservableCollection<ServiceVersion>()
            };

            await LoadCategoriesAsync(settings);
            return settings;
        }

        public async Task ResetSettingsAsync()
        {
            try
            {
                if (await _fileSystemService.FileExistsAsync(SettingsFilePath))
                {
                    await _fileSystemService.DeleteFileAsync(SettingsFilePath);
                }
                _cachedSettings = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to reset settings: {ex.Message}");
                throw;
            }
        }
        private async Task LoadCategoriesAsync(AppSettings settings)
        {
            await Task.Run(() =>
            {
                var services = new[]
                {
                    "Apache", "MySQL", "PHP", "Node", "Redis", "PostgreSQL", "Nginx"
                };

                foreach (var service in services)
                {
                    var existingService = settings.Versions.FirstOrDefault(c => c.Service == service);
                    if (existingService == null)
                    {
                        settings.Versions.Add(new ServiceVersion
                        {
                            Service = service,
                            Version = string.Empty
                        });
                    }
                }
            });
        }

        private AppSettings ParseIniToSettings(IniData iniData)
        {
            var settings = new AppSettings();

            // Parse General section
            if (iniData.Sections.ContainsSection("General"))
            {
                var generalSection = iniData.Sections["General"];
                settings.StartWithWindows = bool.Parse(generalSection["StartWithWindows"] ?? "false");
                settings.MinimizeToSystemTray = bool.Parse(generalSection["MinimizeToSystemTray"] ?? "false");
                settings.AutoVirtualHosts = bool.Parse(generalSection["AutoVirtualHosts"] ?? "true");
                settings.AutoCreateDatabase = bool.Parse(generalSection["AutoCreateDatabase"] ?? "false");
                settings.InstallDirectory = generalSection["InstallDirectory"] ?? @"C:\DevNest";
            }

            // Parse Versions section
            if (iniData.Sections.ContainsSection("Versions"))
            {
                var versionsSection = iniData.Sections["Versions"];
                foreach (var key in versionsSection)
                {
                    settings.Versions.Add(new ServiceVersion
                    {
                        Service = key.KeyName,
                        Version = key.Value ?? string.Empty
                    });
                }
            }

            return settings;
        }

        private IniData ConvertSettingsToIni(AppSettings settings)
        {
            var iniData = new IniData();

            // General section
            iniData.Sections.AddSection("General");
            var generalSection = iniData.Sections["General"];
            generalSection.AddKey("StartWithWindows", settings.StartWithWindows.ToString().ToLower());
            generalSection.AddKey("MinimizeToSystemTray", settings.MinimizeToSystemTray.ToString().ToLower());
            generalSection.AddKey("AutoVirtualHosts", settings.AutoVirtualHosts.ToString().ToLower());
            generalSection.AddKey("AutoCreateDatabase", settings.AutoCreateDatabase.ToString().ToLower());
            generalSection.AddKey("InstallDirectory", settings.InstallDirectory);

            // Versions section
            iniData.Sections.AddSection("Versions");
            var versionsSection = iniData.Sections["Versions"]; foreach (var version in settings.Versions)
            {
                versionsSection.AddKey(version.Service, version.Version ?? string.Empty);
            }

            return iniData;
        }
    }
}
