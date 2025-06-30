using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using IniParser.Model;

namespace DevNest.Services.Settings
{
    public class NodeSettingsService : IServiceSettingsProvider<NodeSettings>
    {
        public string ServiceName => "Node";

        public NodeSettings GetDefaultConfiguration()
        {
            return new NodeSettings
            {
                Version = "",
                DefaultPort = 3000,
                PackageManager = "npm",
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

            serviceSettings.Node.Version = section["Version"] ?? "";

            if (int.TryParse(section["DefaultPort"], out var port))
            {
                serviceSettings.Node.DefaultPort = port;
            }

            serviceSettings.Node.PackageManager = section["PackageManager"] ?? "npm";

            if (bool.TryParse(section["AutoStart"], out var autoStart))
            {
                serviceSettings.Node.AutoStart = autoStart;
            }
        }

        public void SaveToIni(IniData iniData, SettingsModel serviceSettings)
        {
            iniData.Sections.AddSection(ServiceName);
            var section = iniData.Sections[ServiceName];

            section.AddKey("Version", serviceSettings.Node.Version ?? "");
            section.AddKey("DefaultPort", serviceSettings.Node.DefaultPort.ToString());
            section.AddKey("PackageManager", serviceSettings.Node.PackageManager ?? "npm");
            section.AddKey("AutoStart", serviceSettings.Node.AutoStart.ToString().ToLower());
        }
    }
}
