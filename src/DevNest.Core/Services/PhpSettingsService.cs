using DevNest.Core.Files;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using IniParser.Model;

namespace DevNest.Services.Settings
{
    public class PhpSettingsService : IServiceSettingsProvider<PHPSettings>
    {

        private readonly FileSystemManager _fileSystemManager;
        private readonly PathManager _pathManager;

        public PhpSettingsService(FileSystemManager fileSystemManager, PathManager pathManager)
        {
            _fileSystemManager = fileSystemManager;
            _pathManager = pathManager;
        }

        public string ServiceName => "PHP";

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
            }
        }
    }
}
