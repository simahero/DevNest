using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using IniParser.Model;

namespace DevNest.Services.Settings
{
    public class MySqlSettingsService : IServiceSettingsProvider<MySQLSettings>
    {
        public string ServiceName => "MySQL";

        public MySQLSettings GetDefaultConfiguration()
        {
            return new MySQLSettings
            {
                Version = "",
                Port = 3306,
                RootPassword = "",
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

            serviceSettings.MySQL.Version = section["Version"] ?? "";

            if (int.TryParse(section["Port"], out var port))
            {
                serviceSettings.MySQL.Port = port;
            }

            serviceSettings.MySQL.RootPassword = section["RootPassword"] ?? "";

            if (bool.TryParse(section["AutoStart"], out var autoStart))
            {
                serviceSettings.MySQL.AutoStart = autoStart;
            }

            serviceSettings.MySQL.LogLevel = section["LogLevel"] ?? "Info";
        }

        public void SaveToIni(IniData iniData, SettingsModel serviceSettings)
        {
            iniData.Sections.AddSection(ServiceName);
            var section = iniData.Sections[ServiceName];

            section.AddKey("Version", serviceSettings.MySQL.Version ?? "");
            section.AddKey("Port", serviceSettings.MySQL.Port.ToString());
            section.AddKey("RootPassword", serviceSettings.MySQL.RootPassword ?? "");
            section.AddKey("AutoStart", serviceSettings.MySQL.AutoStart.ToString().ToLower());
            section.AddKey("LogLevel", serviceSettings.MySQL.LogLevel ?? "Info");
        }
    }
}
