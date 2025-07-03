using DevNest.Core.Enums;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using IniParser.Model;
using DevNest.Core.Helpers;

namespace DevNest.Core.Services
{
    public class NodeModelService : IServiceSettingsProvider<NodeModel>
    {
        public ServiceType Type => ServiceType.Node;
        public string ServiceName => Type.ToString();

        public NodeModel GetDefaultConfiguration()
        {
            return new NodeModel
            {
                Version = "",
                DefaultPort = 3000,
                PackageManager = "npm",
                AutoStart = false,
            };
        }

        public void ParseFromIni(IniData iniData, Model serviceSettings)
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

        public void SaveToIni(IniData iniData, Model serviceSettings)
        {
            iniData.Sections.AddSection(ServiceName);
            var section = iniData.Sections[ServiceName];

            section.AddKey("Version", serviceSettings.Node.Version ?? "");
            section.AddKey("DefaultPort", serviceSettings.Node.DefaultPort.ToString());
            section.AddKey("PackageManager", serviceSettings.Node.PackageManager ?? "npm");
            section.AddKey("AutoStart", serviceSettings.Node.AutoStart.ToString().ToLower());
        }

        public static async Task<(string, string)> GetCommandAsync(ServiceModel service, Model settings)
        {
            var selectedVersion = settings.Node.Version;
            if (!string.IsNullOrEmpty(selectedVersion))
            {
                return ($"node.exe", service.Path);
            }
            return (string.Empty, string.Empty);
        }
    }
}
