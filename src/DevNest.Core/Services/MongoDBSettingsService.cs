using DevNest.Core.Enums;
using DevNest.Core.Helpers;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using IniParser.Model;

namespace DevNest.Core.Services
{
    public class MongoDBSettingsService : IServiceSettingsProvider<MongoDBSettings>
    {
        public ServiceType Type => ServiceType.MongoDB;
        public string ServiceName => Type.ToString();

        public MongoDBSettingsService() { }

        public MongoDBSettings GetDefaultConfiguration()
        {
            return new MongoDBSettings
            {
                Version = "",
                Port = 27017,
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

            serviceSettings.MongoDB.Version = section["Version"] ?? "";

            if (int.TryParse(section["Port"], out var port))
            {
                serviceSettings.MongoDB.Port = port;
            }

            if (bool.TryParse(section["AutoStart"], out var autoStart))
            {
                serviceSettings.MongoDB.AutoStart = autoStart;
            }
        }

        public void SaveToIni(IniData iniData, SettingsModel serviceSettings)
        {
            iniData.Sections.AddSection(ServiceName);
            var section = iniData.Sections[ServiceName];

            section.AddKey("Version", serviceSettings.MongoDB.Version ?? string.Empty);
            section.AddKey("Port", serviceSettings.MongoDB.Port.ToString());
            section.AddKey("AutoStart", serviceSettings.MongoDB.AutoStart.ToString().ToLower());

            _ = Task.Run(async () =>
            {
                await GenerateMongoDBConfigurationAsync(serviceSettings);
            });
        }

        private async Task GenerateMongoDBConfigurationAsync(SettingsModel settings)
        {
            string TemplateFilePath = Path.Combine(PathManager.TemplatesPath, "mongod.cfg.tpl");

            try
            {

                if (!await FileSystemManager.FileExistsAsync(TemplateFilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"MongoDB template file not found: {TemplateFilePath}");
                    return;
                }

                var templateContent = await FileSystemManager.ReadAllTextAsync(TemplateFilePath);

                var dataDir = Path.Combine(PathManager.DataPath, settings.MongoDB.Version);
                var logPath = Path.Combine(PathManager.LogsPath, settings.MongoDB.Version + ".log");

                var configContent = templateContent
                    .Replace("<<DATADIR>>", dataDir.Replace("\\", "/"))
                    .Replace("<<LOGPATH>>", logPath.Replace("\\", "/"));

                var configDir = Path.Combine(PathManager.BinPath, "MongoDB", settings.MongoDB.Version, "bin");

                var configFilePath = Path.Combine(configDir, "mongod.cfg");
                await FileSystemManager.WriteAllTextAsync(configFilePath, configContent);

                if (!await FileSystemManager.DirectoryExistsAsync(dataDir))
                {
                    await FileSystemManager.CreateDirectoryAsync(dataDir);
                }

                System.Diagnostics.Debug.WriteLine($"MongoDB configuration generated: {configFilePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating MongoDB configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// Returns the command and working directory for MongoDB, or (string.Empty, string.Empty) if not found.
        /// </summary>
        public static async Task<(string, string)> GetCommandAsync(ServiceModel service, SettingsModel settings)
        {
            var selectedVersion = settings.MongoDB.Version;
            if (!string.IsNullOrEmpty(selectedVersion))
            {
                var mongoDBPath = Path.Combine(service.Path, "bin", "mongod.exe");
                var configPath = Path.Combine(service.Path, "bin", "mongod.cfg");

                if (await FileSystemManager.FileExistsAsync(mongoDBPath))
                {
                    return ($"\"{mongoDBPath}\" --config \"{configPath}\"", Path.GetDirectoryName(mongoDBPath)!);
                }
            }
            return (string.Empty, string.Empty);
        }
    }
}
