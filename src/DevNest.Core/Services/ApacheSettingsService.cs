using DevNest.Core.Files;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using IniParser.Model;

namespace DevNest.Services.Settings
{
    public class ApacheSettingsService : IServiceSettingsProvider<ApacheSettings>
    {
        public string ServiceName => "Apache";

        private readonly FileSystemManager _fileSystemManager;
        private readonly PathManager _pathManager;

        public ApacheSettingsService(FileSystemManager fileSystemManager, PathManager pathManager)
        {
            _fileSystemManager = fileSystemManager;
            _pathManager = pathManager;
        }

        public ApacheSettings GetDefaultConfiguration()
        {
            return new ApacheSettings
            {
                Version = "",
                DocumentRoot = _pathManager.WwwPath,
                Port = 80,
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

            serviceSettings.Apache.Version = section["Version"] ?? "";
            serviceSettings.Apache.DocumentRoot = section["DocumentRoot"] ?? _pathManager.WwwPath;

            if (int.TryParse(section["Port"], out var port))
            {
                serviceSettings.Apache.Port = port;
            }

            if (bool.TryParse(section["AutoStart"], out var autoStart))
            {
                serviceSettings.Apache.AutoStart = autoStart;
            }
        }

        public void SaveToIni(IniData iniData, SettingsModel serviceSettings)
        {
            iniData.Sections.AddSection(ServiceName);
            var section = iniData.Sections[ServiceName];

            section.AddKey("Version", serviceSettings.Apache.Version ?? string.Empty);
            section.AddKey("DocumentRoot", serviceSettings.Apache.DocumentRoot ?? _pathManager.WwwPath);
            section.AddKey("Port", serviceSettings.Apache.Port.ToString());
            section.AddKey("AutoStart", serviceSettings.Apache.AutoStart.ToString().ToLower());

            _ = Task.Run(async () =>
            {
                await GenerateApacheConfigurationAsync(serviceSettings);
            });
        }

        private async Task GenerateApacheConfigurationAsync(SettingsModel settings)
        {
            string TemplateFilePath = Path.Combine(_pathManager.TemplatesPath, "httpd.conf.tpl");

            try
            {
                if (string.IsNullOrEmpty(settings.Apache.Version))
                {
                    return;
                }

                if (!await _fileSystemManager.FileExistsAsync(TemplateFilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"Apache template file not found: {TemplateFilePath}");
                    return;
                }

                var templateContent = await _fileSystemManager.ReadAllTextAsync(TemplateFilePath);


                var srvRoot = Path.Combine(_pathManager.BinPath, "Apache", settings.Apache.Version).Replace('\\', '/');
                var documentRoot = settings.Apache.DocumentRoot.Replace('\\', '/');
                var port = settings.Apache.Port.ToString();
                var phpPath = Path.Combine(_pathManager.BinPath, "PHP", settings.PHP.Version).Replace('\\', '/');
                var etcPath = _pathManager.EtcPath.Replace('\\', '/');

                var configContent = templateContent
                    .Replace("<<SRVROOT>>", srvRoot)
                    .Replace("<<PORT>>", port)
                    .Replace("<<DOCUMENTROOT>>", documentRoot)
                    .Replace("<<PHPPATH>>", phpPath)
                    .Replace("<<ETCPATH>>", etcPath);

                var configDir = Path.Combine(srvRoot, "conf");

                var configFilePath = Path.Combine(configDir, "httpd.conf");
                await _fileSystemManager.WriteAllTextAsync(configFilePath, configContent);

                System.Diagnostics.Debug.WriteLine($"Apache configuration generated: {configFilePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating Apache configuration: {ex.Message}");
            }
        }

    }
}
