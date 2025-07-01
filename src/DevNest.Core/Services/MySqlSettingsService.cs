using DevNest.Core.Enums;
using DevNest.Core.Files;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using IniParser.Model;
using System.Diagnostics;

namespace DevNest.Core.Services
{
    public class MySQLSettingsService : IServiceSettingsProvider<MySQLSettings>
    {
        public ServiceType Type => ServiceType.MySQL;
        public string ServiceName => Type.ToString();

        private readonly FileSystemManager _fileSystemManager;
        private readonly PathManager _pathManager;

        public MySQLSettingsService(FileSystemManager fileSystemManager, PathManager pathManager)
        {
            _fileSystemManager = fileSystemManager;
            _pathManager = pathManager;
        }

        public MySQLSettings GetDefaultConfiguration()
        {
            return new MySQLSettings
            {
                Version = "",
                Port = 3306,
                RootPassword = "",
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

            serviceSettings.MySQL.Version = section["Version"] ?? "";

            if (int.TryParse(section["Port"], out var port))
            {
                serviceSettings.MySQL.Port = port;
            }

            serviceSettings.MySQL.RootPassword = section["RootPassword"] ?? "";

            if (bool.TryParse(section["AutoStart"], out var autoStart))
            {
                serviceSettings.MySQL.AutoStart = autoStart;
            }
        }

        public void SaveToIni(IniData iniData, SettingsModel serviceSettings)
        {
            iniData.Sections.AddSection(ServiceName);
            var section = iniData.Sections[ServiceName];

            section.AddKey("Version", serviceSettings.MySQL.Version ?? "");
            section.AddKey("Port", serviceSettings.MySQL.Port.ToString());
            section.AddKey("RootPassword", serviceSettings.MySQL.RootPassword ?? "");
            section.AddKey("AutoStart", serviceSettings.MySQL.AutoStart.ToString().ToLower());

            _ = Task.Run(async () =>
            {
                await GenerateMySQLConfigurationAsync(serviceSettings);
            });

        }

        private async Task GenerateMySQLConfigurationAsync(SettingsModel settings)
        {
            string TemplateFilePath = Path.Combine(_pathManager.TemplatesPath, "mysql.ini.tpl");

            try
            {

                if (!await _fileSystemManager.FileExistsAsync(TemplateFilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"MySQL template file not found: {TemplateFilePath}");
                    return;
                }

                var templateContent = await _fileSystemManager.ReadAllTextAsync(TemplateFilePath);

                var dataDir = Path.Combine(_pathManager.DataPath, settings.MySQL.Version);
                var password = settings.MySQL.RootPassword;
                var port = settings.MySQL.Port.ToString();

                var configContent = templateContent
                    .Replace("<<DATADIR>>", dataDir.Replace("\\", "/"))
                    .Replace("<<PASS>>", password)
                    .Replace("<<PORT>>", port);

                var configDir = Path.Combine(_pathManager.BinPath, "MySQL", settings.MySQL.Version);

                var configFilePath = Path.Combine(configDir, "my.ini");
                await _fileSystemManager.WriteAllTextAsync(configFilePath, configContent);

                var ibdata1Path = Path.Combine(dataDir, "ibdata1");

                if (!await _fileSystemManager.DirectoryExistsAsync(dataDir))
                {
                    await _fileSystemManager.CreateDirectoryAsync(dataDir);
                }

                if (!await _fileSystemManager.FileExistsAsync(ibdata1Path))
                {
                    var binPath = Path.Combine(_pathManager.BinPath, "MySQL", settings.MySQL.Version, "bin");
                    var mysqldPath = Path.Combine(binPath, "mysqld.exe");

                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = mysqldPath,
                            Arguments = "--initialize-insecure --console",
                            WorkingDirectory = binPath,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    process.WaitForExit();
                }


                System.Diagnostics.Debug.WriteLine($"MySQL configuration generated: {configFilePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating MySQL configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// Returns the command and working directory for MySQL, or (string.Empty, string.Empty) if not found.
        /// </summary>
        public static async Task<(string, string)> GetCommandAsync(ServiceModel service, SettingsModel settings, FileSystemManager fileSystemManager)
        {
            var selectedVersion = settings.MySQL.Version;
            if (!string.IsNullOrEmpty(selectedVersion))
            {
                var binPath = Path.Combine(service.Path, "bin");
                var mysqldPath = Path.Combine(binPath, "mysqld.exe");
                if (await fileSystemManager.FileExistsAsync(mysqldPath))
                {
                    return ($"\"{mysqldPath}\"", binPath);
                }
            }
            return (string.Empty, string.Empty);
        }
    }
}
