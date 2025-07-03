using DevNest.Core.Enums;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using IniParser.Model;
using DevNest.Core.Helpers;

namespace DevNest.Core.Services
{
    public class RedisSettingsService : IServiceSettingsProvider<RedisSettings>
    {
        public ServiceType Type => ServiceType.Redis;
        public string ServiceName => Type.ToString();

        public RedisSettings GetDefaultConfiguration()
        {
            return new RedisSettings
            {
                Version = "",
                Port = 6379,
                AutoStart = false,
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

        }

        public void SaveToIni(IniData iniData, SettingsModel serviceSettings)
        {
            iniData.Sections.AddSection(ServiceName);
            var section = iniData.Sections[ServiceName];

            section.AddKey("Version", serviceSettings.Redis.Version ?? "");
            section.AddKey("AutoStart", serviceSettings.Redis.AutoStart.ToString().ToLower());
        }

        public static async Task<(string, string)> GetCommandAsync(ServiceModel service, SettingsModel settings)
        {
            var selectedVersion = settings.Redis.Version;
            if (!string.IsNullOrEmpty(selectedVersion))
            {
                var redisPath = Path.Combine(service.Path, "redis-server.exe");
                if (await FileSystemManager.FileExistsAsync(redisPath))
                {
                    return ($"\"{redisPath}\"", Path.GetDirectoryName(redisPath)!);
                }
            }
            return (string.Empty, string.Empty);
        }
    }
}
