using DevNest.Core.Enums;
using DevNest.Core.Files;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using IniParser.Model;

namespace DevNest.Core.Services
{
    public class PHPSettingsService : IServiceSettingsProvider<PHPSettings>
    {
        public ServiceType Type => ServiceType.PHP;
        public string ServiceName => Type.ToString();

        private readonly FileSystemManager _fileSystemManager;
        private readonly PathManager _pathManager;

        public PHPSettingsService(FileSystemManager fileSystemManager, PathManager pathManager)
        {
            _fileSystemManager = fileSystemManager;
            _pathManager = pathManager;
        }

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

            _ = Task.Run(async () => await GenerateApacheConfigurationAsync(serviceSettings));
        }

        private async Task GenerateApacheConfigurationAsync(SettingsModel settings)
        {
            var phpBinDir = Path.Combine(_pathManager.BinPath, "PHP", settings.PHP.Version);
            var iniDevPath = Path.Combine(phpBinDir, "php.ini-development");
            var iniPath = Path.Combine(phpBinDir, "php.ini");

            if (!await _fileSystemManager.FileExistsAsync(iniDevPath))
            {
                return;
            }

            if (!await _fileSystemManager.FileExistsAsync(iniPath))
            {
                await _fileSystemManager.CopyFileAsync(iniDevPath, iniPath);

                var autoloadPath = Path.Combine(_pathManager.EtcPath, "php", "DevNestDumper", "index.php");
                var prepend = $"auto_prepend_file = {autoloadPath}";

                await _fileSystemManager.AppendAllTextAsync(iniPath, "\n;DEVNEST\n");
                await _fileSystemManager.AppendAllTextAsync(iniPath, $"{prepend}\n");
                await _fileSystemManager.AppendAllTextAsync(iniPath, $"env[VAR_DUMPER_SERVER] = tcp://127.0.0.1:9912\n");
                await _fileSystemManager.AppendAllTextAsync(iniPath, $"env[VAR_DUMPER_FORMAT] = server\n");
                await _fileSystemManager.AppendAllTextAsync(iniPath, $"extension_dir = \"ext\"\n");
                await _fileSystemManager.AppendAllTextAsync(iniPath, $"extension=mysqli\n");

            }

        }
    }
}
