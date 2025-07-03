using DevNest.Core.Enums;
using DevNest.Core.Helpers;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using IniParser.Model;

namespace DevNest.Core.Services
{
    public class PHPModelService : IServiceSettingsProvider<PHPModel>
    {
        public ServiceType Type => ServiceType.PHP;
        public string ServiceName => Type.ToString();

        public PHPModelService() { }

        public PHPModel GetDefaultConfiguration()
        {
            return new PHPModel
            {
                Version = "",
            };
        }

        public void ParseFromIni(IniData iniData, Model serviceSettings)
        {
            if (!iniData.Sections.ContainsSection(ServiceName))
            {
                return;
            }

            var section = iniData.Sections[ServiceName];
            serviceSettings.PHP.Version = section["Version"] ?? "";
        }

        public void SaveToIni(IniData iniData, Model serviceSettings)
        {
            iniData.Sections.AddSection(ServiceName);
            var section = iniData.Sections[ServiceName];

            section.AddKey("Version", serviceSettings.PHP.Version ?? string.Empty);

            _ = Task.Run(async () => await GeneratePHPConfigurationAsync(serviceSettings));
        }

        private async Task GeneratePHPConfigurationAsync(Model settings)
        {
            var phpBinDir = Path.Combine(PathHelper.BinPath, "PHP", settings.PHP.Version);
            var iniDevPath = Path.Combine(phpBinDir, "php.ini-development");
            var iniPath = Path.Combine(phpBinDir, "php.ini");

            if (!await FileSystemHelper.FileExistsAsync(iniDevPath))
            {
                return;
            }

            if (!await FileSystemHelper.FileExistsAsync(iniPath))
            {
                await FileSystemHelper.CopyFileAsync(iniDevPath, iniPath);

                var autoloadPath = Path.Combine(PathHelper.EtcPath, "php", "DevNestDumper", "index.php");
                var prepend = $"auto_prepend_file = {autoloadPath}";

                await FileSystemHelper.AppendAllTextAsync(iniPath, "\n;DEVNEST\n");
                await FileSystemHelper.AppendAllTextAsync(iniPath, $"{prepend}\n");
                await FileSystemHelper.AppendAllTextAsync(iniPath, $"env[VAR_DUMPER_SERVER] = tcp://127.0.0.1:9912\n");
                await FileSystemHelper.AppendAllTextAsync(iniPath, $"env[VAR_DUMPER_FORMAT] = server\n");
                await FileSystemHelper.AppendAllTextAsync(iniPath, $"extension_dir = \"ext\"\n");
                await FileSystemHelper.AppendAllTextAsync(iniPath, $"extension=mysqli\n");

            }

        }

        public static async Task<(string, string)> GetCommandAsync(ServiceModel service, Model settings)
        {
            var selectedVersion = settings.PHP.Version;
            if (!string.IsNullOrEmpty(selectedVersion))
            {
                var binPath = Path.Combine(service.Path, "php-cgi.exe");
                if (await FileSystemHelper.FileExistsAsync(binPath))
                {
                    return ($"\"{binPath}\" -b 127.0.0.1:9003", service.Path);
                }
            }
            return (string.Empty, string.Empty);
        }

    }
}
