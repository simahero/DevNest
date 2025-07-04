using DevNest.Core.Enums;
using DevNest.Core.Helpers;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using IniParser.Model;

namespace DevNest.Core.Services
{
    public class MongoDBSettingsService : IServiceSettingsProvider<MongoDBModel>
    {
        public ServiceType Type => ServiceType.MongoDB;
        public string ServiceName => Type.ToString();

        public MongoDBSettingsService() { }

        public MongoDBModel GetDefaultConfiguration()
        {
            return new MongoDBModel
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
            string TemplateFilePath = Path.Combine(PathHelper.TemplatesPath, "mongod.cfg.tpl");

            try
            {

                if (!await FileSystemHelper.FileExistsAsync(TemplateFilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"MongoDB template file not found: {TemplateFilePath}");
                    return;
                }

                var templateContent = await FileSystemHelper.ReadAllTextAsync(TemplateFilePath);

                var dataDir = Path.Combine(PathHelper.DataPath, settings.MongoDB.Version);
                var logPath = Path.Combine(PathHelper.LogsPath, settings.MongoDB.Version + ".log");

                var configContent = templateContent
                    .Replace("<<DATADIR>>", dataDir.Replace("\\", "/"))
                    .Replace("<<LOGPATH>>", logPath.Replace("\\", "/"));

                var configDir = Path.Combine(PathHelper.BinPath, "MongoDB", settings.MongoDB.Version, "bin");

                var configFilePath = Path.Combine(configDir, "mongod.cfg");
                await FileSystemHelper.WriteAllTextAsync(configFilePath, configContent);

                if (!await FileSystemHelper.DirectoryExistsAsync(dataDir))
                {
                    await FileSystemHelper.CreateDirectoryAsync(dataDir);
                }

                System.Diagnostics.Debug.WriteLine($"MongoDB configuration generated: {configFilePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating MongoDB configuration: {ex.Message}");
            }
        }
    }
}
