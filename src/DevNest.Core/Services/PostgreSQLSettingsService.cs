using DevNest.Core.Enums;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using IniParser.Model;
using DevNest.Core.Helpers;

namespace DevNest.Core.Services
{
    public class PostgreSQLModelService : IServiceSettingsProvider<PostgreSQLModel>
    {
        public ServiceType Type => ServiceType.PostgreSQL;
        public string ServiceName => Type.ToString();

        public PostgreSQLModel GetDefaultConfiguration()
        {
            return new PostgreSQLModel
            {
                Version = "",
                Port = 5432,
                AutoStart = false,
            };
        }

        public void ParseFromIni(IniData iniData, Model serviceSettings)
        {
            if (!iniData.Sections.ContainsSection(ServiceName))
                return;

            var section = iniData.Sections[ServiceName];

            serviceSettings.PostgreSQL.Version = section["Version"] ?? "";

            if (bool.TryParse(section["AutoStart"], out var autoStart))
                serviceSettings.PostgreSQL.AutoStart = autoStart;

        }

        public void SaveToIni(IniData iniData, Model serviceSettings)
        {
            iniData.Sections.AddSection(ServiceName);
            var section = iniData.Sections[ServiceName];

            section.AddKey("Version", serviceSettings.PostgreSQL.Version ?? "");
            section.AddKey("AutoStart", serviceSettings.PostgreSQL.AutoStart.ToString().ToLower());
        }

        public static async Task<(string, string)> GetCommandAsync(ServiceModel service, Model settings)
        {
            var selectedVersion = settings.PostgreSQL.Version;
            if (!string.IsNullOrEmpty(selectedVersion))
            {
                var postgresPath = Path.Combine(service.Path, "bin", "postgres.exe");
                if (await FileSystemHelper.FileExistsAsync(postgresPath))
                {
                    return ($"\"{postgresPath}\"", Path.GetDirectoryName(postgresPath)!);
                }
            }
            return (string.Empty, string.Empty);
        }
    }
}
