using DevNest.Core.Enums;
using DevNest.Core.Helpers;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using IniParser.Model;

namespace DevNest.Core.Services
{
    public class PHPSettingsService : IServiceSettingsProvider<PHPSettings>
    {
        public ServiceType Type => ServiceType.PHP;
        public string ServiceName => Type.ToString();

        public PHPSettingsService() { }

        public PHPSettings GetDefaultConfiguration()
        {
            return new PHPSettings
            {
                Version = "",
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
        }

        public void SaveToIni(IniData iniData, SettingsModel serviceSettings)
        {
            iniData.Sections.AddSection(ServiceName);
            var section = iniData.Sections[ServiceName];

            section.AddKey("Version", serviceSettings.PHP.Version ?? string.Empty);

            _ = Task.Run(async () => await GeneratePHPConfigurationAsync(serviceSettings));
        }

        private async Task GeneratePHPConfigurationAsync(SettingsModel settings)
        {
            var phpBinDir = Path.Combine(PathManager.BinPath, "PHP", settings.PHP.Version);
            var iniDevPath = Path.Combine(phpBinDir, "php.ini-development");
            var iniPath = Path.Combine(phpBinDir, "php.ini");

            if (!await FileSystemManager.FileExistsAsync(iniDevPath))
            {
                return;
            }

            if (!await FileSystemManager.FileExistsAsync(iniPath))
            {
                await FileSystemManager.CopyFileAsync(iniDevPath, iniPath);

                var autoloadPath = Path.Combine(PathManager.EtcPath, "php", "DevNestDumper", "index.php");
                var prepend = $"auto_prepend_file = {autoloadPath}";

                await FileSystemManager.AppendAllTextAsync(iniPath, "\n;DEVNEST\n");
                await FileSystemManager.AppendAllTextAsync(iniPath, $"{prepend}\n");
                await FileSystemManager.AppendAllTextAsync(iniPath, $"env[VAR_DUMPER_SERVER] = tcp://127.0.0.1:9912\n");
                await FileSystemManager.AppendAllTextAsync(iniPath, $"env[VAR_DUMPER_FORMAT] = server\n");
                await FileSystemManager.AppendAllTextAsync(iniPath, $"extension_dir = \"ext\"\n");
                await FileSystemManager.AppendAllTextAsync(iniPath, $"extension=mysqli\n");

            }

        }

        public static async Task<(string, string)> GetCommandAsync(ServiceModel service, SettingsModel settings)
        {
            var selectedVersion = settings.PHP.Version;
            if (!string.IsNullOrEmpty(selectedVersion))
            {
                var binPath = Path.Combine(service.Path, "php-cgi.exe");
                if (await FileSystemManager.FileExistsAsync(binPath))
                {
                    return ($"\"{binPath}\" -b 127.0.0.1:9003", service.Path);
                }
            }
            return (string.Empty, string.Empty);
        }

    }
}
