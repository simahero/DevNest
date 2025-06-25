using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using IniParser.Model;

namespace DevNest.Services.Settings
{
    public class NginxSettingsService : IServiceSettingsProvider<NginxSettings>
    {
        public string ServiceName => "Nginx";

        public NginxSettings GetDefaultConfiguration()
        {
            return new NginxSettings
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

            serviceSettings.Nginx.Version = section["Version"] ?? "";

            if (bool.TryParse(section["AutoStart"], out var autoStart))
            {
                serviceSettings.Nginx.AutoStart = autoStart;
            }

            serviceSettings.Nginx.LogLevel = section["LogLevel"] ?? "Info";
        }

        public void SaveToIni(IniData iniData, SettingsModel serviceSettings)
        {
            iniData.Sections.AddSection(ServiceName);
            var section = iniData.Sections[ServiceName];

            section.AddKey("Version", serviceSettings.Nginx.Version ?? "");
            section.AddKey("AutoStart", serviceSettings.Nginx.AutoStart.ToString().ToLower());
            section.AddKey("LogLevel", serviceSettings.Nginx.LogLevel ?? "Info");
        }
    }
}
