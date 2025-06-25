using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using IniParser.Model;

namespace DevNest.Services.Settings
{
    public class PostgreSQLSettingsService : IServiceSettingsProvider<PostgreSQLSettings>
    {
        public string ServiceName => "PostgreSQL";

        public PostgreSQLSettings GetDefaultConfiguration()
        {
            return new PostgreSQLSettings
            {
                Version = "",
                AutoStart = false,
                LogLevel = "Info"
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

            serviceSettings.PostgreSQL.LogLevel = section["LogLevel"] ?? "Info";
        }

        public void SaveToIni(IniData iniData, SettingsModel serviceSettings)
        {
            iniData.Sections.AddSection(ServiceName);
            var section = iniData.Sections[ServiceName];

            section.AddKey("Version", serviceSettings.PostgreSQL.Version ?? "");
            section.AddKey("AutoStart", serviceSettings.PostgreSQL.AutoStart.ToString().ToLower());
            section.AddKey("LogLevel", serviceSettings.PostgreSQL.LogLevel ?? "Info");
        }
    }
}
