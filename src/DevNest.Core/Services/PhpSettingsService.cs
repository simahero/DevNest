using DevNest.Core.Enums;
using DevNest.Core.Helpers;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using IniParser.Model;
using IniParser;

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
                var version = settings.PHP.Version.Replace("PHP", "php_xdebug");
                var xDebug = Path.Combine(PathHelper.EtcPath, "php", "xDebug", version + ".dll");

                var iniContent = await FileSystemHelper.ReadAllTextAsync(iniPath);
                var parser = new FileIniDataParser();
                var iniData = parser.Parser.Parse(iniContent);


                iniData.Global["auto_prepend_file"] = autoloadPath;
                iniData.Global["env[VAR_DUMPER_SERVER]"] = "tcp://127.0.0.1:9912";
                iniData.Global["env[VAR_DUMPER_FORMAT]"] = "server";
                iniData.Global["extension_dir"] = "ext";
                iniData.Global["extension"] = "mysqli";


                if (!iniData.Sections.ContainsSection("Xdebug"))
                {
                    iniData.Sections.AddSection("Xdebug");
                }

                var xdebugSection = iniData.Sections["Xdebug"];
                xdebugSection["zend_extension"] = xDebug;
                xdebugSection["xdebug.mode"] = "debug";
                xdebugSection["xdebug.start_with_request"] = "yes";
                xdebugSection["xdebug.client_host"] = "127.0.0.1";
                xdebugSection["xdebug.client_port"] = "9003";

                parser.WriteFile(iniPath, iniData);

            }

        }
    }
}
