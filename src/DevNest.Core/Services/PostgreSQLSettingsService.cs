using DevNest.Core.Enums;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using IniParser.Model;

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

        public void ParseFromIni(IniData iniData, SettingsModel serviceSettings)
        {
            if (!iniData.Sections.ContainsSection(ServiceName))
                return;

            var section = iniData.Sections[ServiceName];

            serviceSettings.PostgreSQL.Version = section["Version"] ?? "";

            if (bool.TryParse(section["AutoStart"], out var autoStart))
                serviceSettings.PostgreSQL.AutoStart = autoStart;

        }

        public void SaveToIni(IniData iniData, SettingsModel serviceSettings)
        {
            iniData.Sections.AddSection(ServiceName);
            var section = iniData.Sections[ServiceName];

            section.AddKey("Version", serviceSettings.PostgreSQL.Version ?? "");
            section.AddKey("AutoStart", serviceSettings.PostgreSQL.AutoStart.ToString().ToLower());
        }
    }
}
