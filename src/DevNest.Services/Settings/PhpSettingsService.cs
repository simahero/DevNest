using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using IniParser.Model;

namespace DevNest.Services.Settings
{
    public class PhpSettingsService : IServiceSettingsProvider<PHPSettings>
    {
        public string ServiceName => "PHP";

        public PHPSettings GetDefaultConfiguration()
        {
            return new PHPSettings
            {
                Version = "",
                MemoryLimit = "256M",
                MaxExecutionTime = 30,
                AutoStart = true,
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

            serviceSettings.PHP.Version = section["Version"] ?? "";
            serviceSettings.PHP.MemoryLimit = section["MemoryLimit"] ?? "256M";

            if (int.TryParse(section["MaxExecutionTime"], out var maxExecTime))
            {
                serviceSettings.PHP.MaxExecutionTime = maxExecTime;
            }

            if (bool.TryParse(section["AutoStart"], out var autoStart))
            {
                serviceSettings.PHP.AutoStart = autoStart;
            }

            serviceSettings.PHP.LogLevel = section["LogLevel"] ?? "Info";
        }

        public void SaveToIni(IniData iniData, SettingsModel serviceSettings)
        {
            iniData.Sections.AddSection(ServiceName);
            var section = iniData.Sections[ServiceName];

            section.AddKey("Version", serviceSettings.PHP.Version ?? string.Empty);
            section.AddKey("MemoryLimit", serviceSettings.PHP.MemoryLimit ?? "256M");
            section.AddKey("MaxExecutionTime", serviceSettings.PHP.MaxExecutionTime.ToString());
            section.AddKey("AutoStart", serviceSettings.PHP.AutoStart.ToString().ToLower());
            section.AddKey("LogLevel", serviceSettings.PHP.LogLevel ?? "Info");
        }

    }
}
