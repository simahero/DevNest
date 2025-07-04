using DevNest.Core.Enums;
using DevNest.Core.Helpers;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using DevNest.Core.State;
using IniParser.Model;

namespace DevNest.Core.Services
{
    public class NginxSettingsService : IServiceSettingsProvider<NginxModel>
    {
        public ServiceType Type => ServiceType.Nginx;
        public string ServiceName => Type.ToString();

        private readonly AppState _appState;

        public NginxSettingsService(AppState appState)
        {
            _appState = appState;
        }

        public NginxModel GetDefaultConfiguration()
        {
            return new NginxModel
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
            string TemplateFilePath = Path.Combine(PathHelper.TemplatesPath, "nginx.conf.tpl");

            try
            {
                if (string.IsNullOrEmpty(settings.Nginx.Version))
                {
                    return;
                }

                if (!await FileSystemHelper.FileExistsAsync(TemplateFilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"Nginx template file not found: {TemplateFilePath}");
                    return;
                }

                var templateContent = await FileSystemHelper.ReadAllTextAsync(TemplateFilePath);


                var srvRoot = Path.Combine(PathHelper.BinPath, "Nginx", settings.Nginx.Version).Replace('\\', '/');
                var logsPath = Path.Combine(PathHelper.LogsPath).Replace('\\', '/');
                var etcPath = PathHelper.EtcPath.Replace('\\', '/');

                var configContent = templateContent
                    .Replace("<<LOGSPATH>>", logsPath)
                    .Replace("<<ETCPATH>>", etcPath);

                var configDir = Path.Combine(srvRoot, "conf");

                var configFilePath = Path.Combine(configDir, "nginx.conf");
                await FileSystemHelper.WriteAllTextAsync(configFilePath, configContent);

                var tempPath = Path.Combine(srvRoot, "temp", "client_body_temp");
                var nginxLogPath = Path.Combine(srvRoot, "logs");

                if (!Directory.Exists(tempPath))
                {
                    await FileSystemHelper.CreateDirectoryAsync(tempPath);
                }

                if (!Directory.Exists(nginxLogPath))
                {
                    await FileSystemHelper.CreateDirectoryAsync(nginxLogPath);
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
            var sites = _appState.Sites;
            var nginxTemplatePath = Path.Combine(PathHelper.TemplatesPath, "auto.nginx.sites-enabled.conf.tpl");

            var templateContent = await FileSystemHelper.ReadAllTextAsync(nginxTemplatePath);

            var nginxSitesEnabledPath = Path.Combine(PathHelper.EtcPath, "nginx", "sites-enabled");

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
                    var srvRoot = Path.Combine(PathHelper.BinPath, "Nginx", settings.Nginx.Version).Replace('\\', '/');

                    var processedContent = templateContent
                        .Replace("<<PORT>>", settings.Nginx.Port.ToString())
                        .Replace("<<PROJECT_DIR>>", Path.Combine(PathHelper.WwwPath, site.Name).Replace('\\', '/'))
                        .Replace("<<HOSTNAME>>", $"{site.Name}.test")
                        .Replace("<<SRVROOT>>", srvRoot);

                    var nginxConfigFilePath = Path.Combine(nginxSitesEnabledPath, $"auto.{site.Name}.test.conf");
                    await FileSystemHelper.WriteAllTextAsync(nginxConfigFilePath, processedContent);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to process template: {ex.Message}");
                }
            }
        }
    }
}
