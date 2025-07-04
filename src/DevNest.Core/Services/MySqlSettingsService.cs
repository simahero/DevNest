using DevNest.Core.Enums;
using DevNest.Core.Helpers;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using IniParser.Model;
using System.Diagnostics;

namespace DevNest.Core.Services
{
    public class MySQLSettingsService : IServiceSettingsProvider<MySQLModel>
    {
        public ServiceType Type => ServiceType.MySQL;
        public string ServiceName => Type.ToString();

        public MySQLSettingsService() { }

        public MySQLModel GetDefaultConfiguration()
        {
            return new MySQLModel
            {
                Version = "",
                Port = 3306,
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
            section.AddKey("AutoStart", serviceSettings.MySQL.AutoStart.ToString().ToLower());

            _ = Task.Run(async () =>
            {
                await GenerateMySQLConfigurationAsync(serviceSettings);
            });

        }

        private async Task GenerateMySQLConfigurationAsync(SettingsModel settings)
        {
            string TemplateFilePath = Path.Combine(PathHelper.TemplatesPath, "mysql.ini.tpl");

            try
            {

                if (!await FileSystemHelper.FileExistsAsync(TemplateFilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"MySQL template file not found: {TemplateFilePath}");
                    return;
                }

                var templateContent = await FileSystemHelper.ReadAllTextAsync(TemplateFilePath);

                var dataDir = Path.Combine(PathHelper.DataPath, settings.MySQL.Version);
                var port = settings.MySQL.Port.ToString();

                var configContent = templateContent
                    .Replace("<<DATADIR>>", dataDir.Replace("\\", "/"))
                    .Replace("<<PORT>>", port);

                var configDir = Path.Combine(PathHelper.BinPath, "MySQL", settings.MySQL.Version);

                var configFilePath = Path.Combine(configDir, "my.ini");
                await FileSystemHelper.WriteAllTextAsync(configFilePath, configContent);

                var ibdata1Path = Path.Combine(dataDir, "ibdata1");

                if (!await FileSystemHelper.DirectoryExistsAsync(dataDir))
                {
                    await FileSystemHelper.CreateDirectoryAsync(dataDir);
                }

                if (!await FileSystemHelper.FileExistsAsync(ibdata1Path))
                {
                    var binPath = Path.Combine(PathHelper.BinPath, "MySQL", settings.MySQL.Version, "bin");
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
    }
}
