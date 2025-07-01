using DevNest.Core.Enums;
using DevNest.Core.Files;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using IniParser.Model;

namespace DevNest.Core.Services
{
    public class NginxSettingsService : IServiceSettingsProvider<NginxSettings>
    {
        public ServiceType Type => ServiceType.Nginx;
        public string ServiceName => Type.ToString();

        private readonly FileSystemManager _fileSystemManager;
        private readonly PathManager _pathManager;

        public NginxSettingsService(FileSystemManager fileSystemManager, PathManager pathManager)
        {
            _fileSystemManager = fileSystemManager;
            _pathManager = pathManager;
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
            section.AddKey("AutoStart", serviceSettings.Nginx.AutoStart.ToString().ToLower());

            // _ = Task.Run(async () =>
            // {
            //     await GenerateNginxConfigurationAsync(serviceSettings);
            // });
        }

        /// <summary>
        /// Returns the command and working directory for Nginx, or (string.Empty, string.Empty) if not found.
        /// </summary>
        public static async Task<(string, string)> GetCommandAsync(ServiceModel service, SettingsModel settings, FileSystemManager fileSystemManager)
        {
            var selectedVersion = settings.Nginx.Version;
            if (!string.IsNullOrEmpty(selectedVersion))
            {
                var nginxPath = Path.Combine(service.Path, "nginx.exe");
                if (await fileSystemManager.FileExistsAsync(nginxPath))
                {
                    return ($"\"{nginxPath}\"", Path.GetDirectoryName(nginxPath)!);
                }
            }
            return (string.Empty, string.Empty);
        }
    }
}
