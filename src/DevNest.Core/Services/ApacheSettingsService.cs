using DevNest.Core.Enums;
using DevNest.Core.Files;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using IniParser.Model;

namespace DevNest.Core.Services
{
    public class ApacheSettingsService : IServiceSettingsProvider<ApacheSettings>
    {
        public ServiceType Type => ServiceType.Apache;
        public string ServiceName => Type.ToString();

        private readonly IServiceProvider _serviceProvider;

        public ApacheSettingsService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ApacheSettings GetDefaultConfiguration()
        {
            return new ApacheSettings
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
            string TemplateFilePath = Path.Combine(PathManager.TemplatesPath, "httpd.conf.tpl");

            try
            {
                if (string.IsNullOrEmpty(settings.Apache.Version))
                {
                    return;
                }

                if (!await FileSystemManager.FileExistsAsync(TemplateFilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"Apache template file not found: {TemplateFilePath}");
                    return;
                }

                var templateContent = await FileSystemManager.ReadAllTextAsync(TemplateFilePath);


                var srvRoot = Path.Combine(PathManager.BinPath, "Apache", settings.Apache.Version).Replace('\\', '/');
                var logsPath = Path.Combine(PathManager.LogsPath).Replace('\\', '/');
                var documentRoot = settings.Apache.DocumentRoot.Replace('\\', '/');
                var port = settings.Apache.Port.ToString();
                var phpPath = Path.Combine(PathManager.BinPath, "PHP", settings.PHP.Version).Replace('\\', '/');
                var etcPath = PathManager.EtcPath.Replace('\\', '/');

                var configContent = templateContent
                    .Replace("<<SRVROOT>>", srvRoot)
                    .Replace("<<LOGSPATH>>", logsPath)
                    .Replace("<<PORT>>", port)
                    .Replace("<<DOCUMENTROOT>>", documentRoot)
                    .Replace("<<PHPPATH>>", phpPath)
                    .Replace("<<ETCPATH>>", etcPath);

                var configDir = Path.Combine(srvRoot, "conf");

                var configFilePath = Path.Combine(configDir, "httpd.conf");
                await FileSystemManager.WriteAllTextAsync(configFilePath, configContent);

                System.Diagnostics.Debug.WriteLine($"Apache configuration generated: {configFilePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating Apache configuration: {ex.Message}");
            }
        }

        private async Task GenerateApacheVirtualHosts(SettingsModel settings)
        {
            var siteManager = (SiteManager?)_serviceProvider.GetService(typeof(SiteManager));
            if (siteManager == null)
            {
                throw new InvalidOperationException("SiteManager service is not registered in the service provider.");
            }
            var sites = await siteManager.GetInstalledSitesAsync();
            var apacheTemplatePath = Path.Combine(PathManager.TemplatesPath, "auto.apache.sites-enabled.conf.tpl");

            var templateContent = await FileSystemManager.ReadAllTextAsync(apacheTemplatePath);

            var apacheSitesEnabledPath = Path.Combine(PathManager.EtcPath, "apache", "sites-enabled");

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
                        .Replace("<<PROJECT_DIR>>", Path.Combine(PathManager.WwwPath, site.Name))
                        .Replace("<<HOSTNAME>>", $"{site.Name}.test")
                        .Replace("<<SITENAME>>", site.Name);

                    var apacheConfigFilePath = Path.Combine(apacheSitesEnabledPath, $"auto.{site.Name}.test.conf");
                    await FileSystemManager.WriteAllTextAsync(apacheConfigFilePath, processedContent);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to process template: {ex.Message}");
                }
            }
        }

        public static async Task<(string, string)> GetCommandAsync(ServiceModel service, SettingsModel settings)
        {
            var selectedVersion = settings.Apache.Version;
            if (!string.IsNullOrEmpty(selectedVersion))
            {
                var binPath = Path.Combine(service.Path, "bin");
                var apacheRoot = service.Path;
                var httpdPath = Path.Combine(binPath, "httpd.exe");
                if (await FileSystemManager.FileExistsAsync(httpdPath))
                {
                    return ($"\"{httpdPath}\" -d \"{apacheRoot}\" -D FOREGROUND", binPath);
                }
            }
            return (string.Empty, string.Empty);
        }
    }
}
