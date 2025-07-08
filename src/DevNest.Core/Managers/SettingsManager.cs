using DevNest.Core.Helpers;
using DevNest.Core.Models;
using DevNest.Core.Services;
using IniParser.Model;
using IniParser.Parser;

namespace DevNest.Core
{
    public class SettingsManager
    {
        private readonly SettingsFactory _settingsFactory;

        public SettingsManager(SettingsFactory settingsFactory)
        {
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
