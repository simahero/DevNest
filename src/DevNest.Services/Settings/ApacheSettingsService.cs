using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using IniParser.Model;
using System.Text;

namespace DevNest.Services.Settings
{
    public class ApacheSettingsService : IServiceSettingsProvider<ApacheSettings>
    {
        private readonly IFileSystemService _fileSystemService;
        private readonly IPathService _pathService;
        private static readonly string TemplateFilePath = @"C:\DevNest\template\httpd.conf.tpl";

        public ApacheSettingsService(IFileSystemService fileSystemService, IPathService pathService)
        {
            _fileSystemService = fileSystemService;
            _pathService = pathService;
        }

        public string ServiceName => "Apache";

        public ApacheSettings GetDefaultConfiguration()
        {
            return new ApacheSettings
            {
                Version = "",
                DocumentRoot = @"C:\DevNest\wwww",
                ListenPort = 80,
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

            serviceSettings.Apache.Version = section["Version"] ?? "";
            serviceSettings.Apache.DocumentRoot = section["DocumentRoot"] ?? @"C:\DevNest\www";

            if (int.TryParse(section["ListenPort"], out var port))
            {
                serviceSettings.Apache.ListenPort = port;
            }

            if (bool.TryParse(section["AutoStart"], out var autoStart))
            {
                serviceSettings.Apache.AutoStart = autoStart;
            }

            serviceSettings.Apache.LogLevel = section["LogLevel"] ?? "Info";
        }

        public void SaveToIni(IniData iniData, SettingsModel serviceSettings)
        {
            iniData.Sections.AddSection(ServiceName);
            var section = iniData.Sections[ServiceName];

            section.AddKey("Version", serviceSettings.Apache.Version ?? string.Empty);
            section.AddKey("DocumentRoot", serviceSettings.Apache.DocumentRoot ?? @"C:\DevNest\wwww");
            section.AddKey("ListenPort", serviceSettings.Apache.ListenPort.ToString());
            section.AddKey("AutoStart", serviceSettings.Apache.AutoStart.ToString().ToLower());
            section.AddKey("LogLevel", serviceSettings.Apache.LogLevel ?? "Info");

            // Generate httpd.conf file from template when settings are saved
            _ = Task.Run(async () => await GenerateApacheConfigurationAsync(serviceSettings));
        }

        private async Task GenerateApacheConfigurationAsync(SettingsModel settings)
        {
            try
            {
                if (string.IsNullOrEmpty(settings.Apache.Version))
                {
                    return;
                }

                if (!await _fileSystemService.FileExistsAsync(TemplateFilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"Apache template file not found: {TemplateFilePath}");
                    return;
                }

                var templateContent = await _fileSystemService.ReadAllTextAsync(TemplateFilePath);


                var srvRoot = Path.Combine(_pathService.BinPath, "Apache", settings.Apache.Version).Replace('\\', '/');
                var documentRoot = settings.Apache.DocumentRoot.Replace('\\', '/');
                var port = settings.Apache.ListenPort.ToString();
                var phpPath = Path.Combine(_pathService.BinPath, "PHP", settings.PHP.Version).Replace('\\', '/');

                var configContent = templateContent
                    .Replace("<<SRVROOT>>", srvRoot)
                    .Replace("<<PORT>>", port)
                    .Replace("<<DOCUMENTROOT>>", documentRoot)
                    .Replace("<<PHPPATH>>", phpPath);

                var configDir = Path.Combine(srvRoot, "conf");
                if (!await _fileSystemService.DirectoryExistsAsync(configDir))
                {
                    await _fileSystemService.CreateDirectoryAsync(configDir);
                }

                var configFilePath = Path.Combine(configDir, "httpd.conf");
                await _fileSystemService.WriteAllTextAsync(configFilePath, configContent);

                System.Diagnostics.Debug.WriteLine($"Apache configuration generated: {configFilePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating Apache configuration: {ex.Message}");
            }
        }


    }
}
