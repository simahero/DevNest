using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using IniParser.Model;

namespace DevNest.Services.Settings
{
    public class RedisSettingsService : IServiceSettingsProvider<RedisSettings>
    {
        public string ServiceName => "Redis";

        public RedisSettings GetDefaultConfiguration()
        {
            return new RedisSettings
            {
                Version = "",
                AutoStart = false,
                LogLevel = "Info"
            };
        }

        public void ParseFromIni(IniData iniData, SettingsModel serviceSettings)
        {
            if (!iniData.Sections.ContainsSection(ServiceName))
            {
                return;
            }

            var section = iniData.Sections[ServiceName];

            serviceSettings.Redis.Version = section["Version"] ?? "";

            if (bool.TryParse(section["AutoStart"], out var autoStart))
            {
                serviceSettings.Redis.AutoStart = autoStart;
            }

            serviceSettings.Redis.LogLevel = section["LogLevel"] ?? "Info";
        }

        public void SaveToIni(IniData iniData, SettingsModel serviceSettings)
        {
            iniData.Sections.AddSection(ServiceName);
            var section = iniData.Sections[ServiceName];

            section.AddKey("Version", serviceSettings.Redis.Version ?? "");
            section.AddKey("AutoStart", serviceSettings.Redis.AutoStart.ToString().ToLower());
            section.AddKey("LogLevel", serviceSettings.Redis.LogLevel ?? "Info");
        }
    }
}
