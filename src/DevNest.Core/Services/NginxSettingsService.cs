using DevNest.Core.Enums;
using DevNest.Core.Helpers;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using IniParser.Model;

namespace DevNest.Core.Services
{
    public class NginxSettingsService : IServiceSettingsProvider<NginxSettings>
    {
        public ServiceType Type => ServiceType.Nginx;
        public string ServiceName => Type.ToString();

        private readonly IServiceProvider _serviceProvider;

        public NginxSettingsService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public NginxSettings GetDefaultConfiguration()
        {
            return new NginxSettings
            {
                Version = "",
                Port = 8080,
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

            serviceSettings.Nginx.Version = section["Version"] ?? "";
            if (int.TryParse(section["Port"], out var port))
            {
                serviceSettings.Nginx.Port = port;
            }

            if (bool.TryParse(section["AutoStart"], out var autoStart))
            {
                serviceSettings.Nginx.AutoStart = autoStart;
            }

        }

        public void SaveToIni(IniData iniData, SettingsModel serviceSettings)
        {
            iniData.Sections.AddSection(ServiceName);
            var section = iniData.Sections[ServiceName];

            section.AddKey("Version", serviceSettings.Nginx.Version ?? "");
            section.AddKey("Port", serviceSettings.Nginx.Port.ToString() ?? "80");
            section.AddKey("AutoStart", serviceSettings.Nginx.AutoStart.ToString().ToLower());

            _ = Task.Run(async () =>
            {
                await GenerateNginxConfigurationAsync(serviceSettings);
                await GenerateNginxVirtualHosts(serviceSettings);
            });
        }

        private async Task GenerateNginxConfigurationAsync(SettingsModel settings)
        {
            string TemplateFilePath = Path.Combine(PathManager.TemplatesPath, "nginx.conf.tpl");

            try
            {
                if (string.IsNullOrEmpty(settings.Nginx.Version))
                {
                    return;
                }

                if (!await FileSystemManager.FileExistsAsync(TemplateFilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"Nginx template file not found: {TemplateFilePath}");
                    return;
                }

                var templateContent = await FileSystemManager.ReadAllTextAsync(TemplateFilePath);


                var srvRoot = Path.Combine(PathManager.BinPath, "Nginx", settings.Nginx.Version).Replace('\\', '/');
                var logsPath = Path.Combine(PathManager.LogsPath).Replace('\\', '/');
                var etcPath = PathManager.EtcPath.Replace('\\', '/');

                var configContent = templateContent
                    .Replace("<<LOGSPATH>>", logsPath)
                    .Replace("<<ETCPATH>>", etcPath);

                var configDir = Path.Combine(srvRoot, "conf");

                var configFilePath = Path.Combine(configDir, "nginx.conf");
                await FileSystemManager.WriteAllTextAsync(configFilePath, configContent);

                var tempPath = Path.Combine(srvRoot, "temp", "client_body_temp");
                var nginxLogPath = Path.Combine(srvRoot, "logs");

                if (!Directory.Exists(tempPath))
                {
                    await FileSystemManager.CreateDirectoryAsync(tempPath);
                }

                if (!Directory.Exists(nginxLogPath))
                {
                    await FileSystemManager.CreateDirectoryAsync(nginxLogPath);
                }

                System.Diagnostics.Debug.WriteLine($"Nginx configuration generated: {configFilePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating Nginx configuration: {ex.Message}");
            }
        }

        private async Task GenerateNginxVirtualHosts(SettingsModel settings)
        {
            var siteManager = (SiteManager?)_serviceProvider.GetService(typeof(SiteManager));

            if (siteManager == null)
            {
                throw new InvalidOperationException("SiteManager service is not registered in the service provider.");
            }

            var sites = await siteManager.GetInstalledSitesAsync();
            var nginxTemplatePath = Path.Combine(PathManager.TemplatesPath, "auto.nginx.sites-enabled.conf.tpl");

            var templateContent = await FileSystemManager.ReadAllTextAsync(nginxTemplatePath);

            var nginxSitesEnabledPath = Path.Combine(PathManager.EtcPath, "nginx", "sites-enabled");

            if (Directory.Exists(nginxSitesEnabledPath))
            {
                foreach (var file in Directory.GetFiles(nginxSitesEnabledPath))
                {
                    File.Delete(file);
                }
                foreach (var dir in Directory.GetDirectories(nginxSitesEnabledPath))
                {
                    Directory.Delete(dir, true);
                }
            }

            foreach (var site in sites)
            {
                try
                {
                    var srvRoot = Path.Combine(PathManager.BinPath, "Nginx", settings.Nginx.Version).Replace('\\', '/');

                    var processedContent = templateContent
                        .Replace("<<PORT>>", settings.Nginx.Port.ToString())
                        .Replace("<<PROJECT_DIR>>", Path.Combine(PathManager.WwwPath, site.Name).Replace('\\', '/'))
                        .Replace("<<HOSTNAME>>", $"{site.Name}.test")
                        .Replace("<<SRVROOT>>", srvRoot);

                    var nginxConfigFilePath = Path.Combine(nginxSitesEnabledPath, $"auto.{site.Name}.test.conf");
                    await FileSystemManager.WriteAllTextAsync(nginxConfigFilePath, processedContent);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to process template: {ex.Message}");
                }
            }
        }

        public static async Task<(string, string)> GetCommandAsync(ServiceModel service, SettingsModel settings)
        {
            var selectedVersion = settings.Nginx.Version;
            if (!string.IsNullOrEmpty(selectedVersion))
            {
                var nginxPath = Path.Combine(service.Path, "nginx.exe");
                if (await FileSystemManager.FileExistsAsync(nginxPath))
                {
                    return ($"\"{nginxPath}\"", Path.GetDirectoryName(nginxPath)!);
                }
            }
            return (string.Empty, string.Empty);
        }
    }
}
