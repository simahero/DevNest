using DevNest.Core.Enums;
using DevNest.Core.Helpers;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using IniParser.Model;

namespace DevNest.Core.Services
{
    public class ApacheSettingsService : IServiceSettingsProvider<ApacheModel>
    {
        public ServiceType Type => ServiceType.Apache;
        public string ServiceName => Type.ToString();

        private readonly ISettingsRepository _settingsRepository;
        private readonly ISiteRepository _siteRepository;

        public ApacheSettingsService(ISettingsRepository settingsRepository, ISiteRepository siteRepository)
        {
            _settingsRepository = settingsRepository;
            _siteRepository = siteRepository;
        }

        public ApacheModel GetDefaultConfiguration()
        {
            return new ApacheModel
            {
                Version = "",
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
            section.AddKey("Port", serviceSettings.Apache.Port.ToString());
            section.AddKey("AutoStart", serviceSettings.Apache.AutoStart.ToString().ToLower());

            _ = Task.Run(async () =>
            {
                await GenerateApacheConfigurationAsync(serviceSettings);
                await GenerateApacheVirtualHosts(serviceSettings);
            });
        }

        private async Task GenerateApacheConfigurationAsync(SettingsModel settings)
        {
            string TemplateFilePath = Path.Combine(PathHelper.TemplatesPath, "httpd.conf.tpl");

            try
            {
                if (string.IsNullOrEmpty(settings.Apache.Version))
                {
                    return;
                }

                if (!await FileSystemHelper.FileExistsAsync(TemplateFilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"Apache template file not found: {TemplateFilePath}");
                    return;
                }

                var templateContent = await FileSystemHelper.ReadAllTextAsync(TemplateFilePath);

                var srvRoot = Path.Combine(PathHelper.BinPath, "Apache", settings.Apache.Version).Replace('\\', '/');
                var wwwPath = PathHelper.WwwPath.Replace('\\', '/');
                var logsPath = Path.Combine(PathHelper.LogsPath).Replace('\\', '/');
                var port = settings.Apache.Port.ToString();
                var phpPath = Path.Combine(PathHelper.BinPath, "PHP", settings.PHP.Version).Replace('\\', '/');
                var etcPath = PathHelper.EtcPath.Replace('\\', '/');

                var configContent = templateContent
                    .Replace("<<SRVROOT>>", srvRoot)
                    .Replace("<<DOCUMENTROOT>>", wwwPath)
                    .Replace("<<LOGSPATH>>", logsPath)
                    .Replace("<<PORT>>", port)
                    .Replace("<<PHPPATH>>", phpPath)
                    .Replace("<<ETCPATH>>", etcPath);

                var configDir = Path.Combine(srvRoot, "conf");

                var configFilePath = Path.Combine(configDir, "httpd.conf");
                await FileSystemHelper.WriteAllTextAsync(configFilePath, configContent);

                System.Diagnostics.Debug.WriteLine($"Apache configuration generated: {configFilePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating Apache configuration: {ex.Message}");
            }
        }

        private async Task GenerateApacheVirtualHosts(SettingsModel settings)
        {
            var sites = await _siteRepository.GetSitesAsync();
            var apacheTemplatePath = Path.Combine(PathHelper.TemplatesPath, "auto.apache.sites-enabled.conf.tpl");

            var templateContent = await FileSystemHelper.ReadAllTextAsync(apacheTemplatePath);

            var apacheSitesEnabledPath = Path.Combine(PathHelper.EtcPath, "apache2", "sites-enabled");
            var phpPath = Path.Combine(PathHelper.BinPath, "PHP", settings.PHP.Version).Replace('\\', '/');

            if (Directory.Exists(apacheSitesEnabledPath))
            {
                foreach (var file in Directory.GetFiles(apacheSitesEnabledPath))
                {
                    File.Delete(file);
                }
                foreach (var dir in Directory.GetDirectories(apacheSitesEnabledPath))
                {
                    Directory.Delete(dir, true);
                }
            }

            foreach (var site in sites)
            {
                try
                {
                    var processedContent = templateContent
                        .Replace("<<PORT>>", settings.Apache.Port.ToString())
                        .Replace("<<PROJECT_DIR>>", Path.Combine(PathHelper.WwwPath, site.Name).Replace('\\', '/'))
                        .Replace("<<PHPPATH>>", phpPath)
                        .Replace("<<HOSTNAME>>", $"{site.Name}.test")
                        .Replace("<<SITENAME>>", site.Name);

                    var apacheConfigFilePath = Path.Combine(apacheSitesEnabledPath, $"auto.{site.Name}.test.conf");
                    await FileSystemHelper.WriteAllTextAsync(apacheConfigFilePath, processedContent);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to process template: {ex.Message}");
                }
            }
        }
    }
}
