using DevNest.Core.Commands;
using DevNest.Core.Enums;
using DevNest.Core.Helpers;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using DevNest.Core.Services;
using IniParser;
using System.Diagnostics;


namespace DevNest.Core
{
    public class ServiceManager
    {
        private readonly SettingsManager _settingsManager;
        private readonly CommandManager _commandManager;
        private readonly IUIDispatcher _uiDispatcher;

        public ServiceManager(SettingsManager settingsManager, CommandManager commandManager, IUIDispatcher uiDispatcher)
        {
            _settingsManager = settingsManager;
            _commandManager = commandManager;
            _uiDispatcher = uiDispatcher;
        }

        public async Task<IEnumerable<ServiceModel>> GetServicesAsync()
        {
            try
            {
                var allServices = new List<ServiceModel>();

                var settings = await _settingsManager.LoadSettingsAsync();
                var binDirectory = PathManager.BinPath;
                var categoryDirectories = await FileSystemManager.GetDirectoriesAsync(binDirectory);
                foreach (var categoryDir in categoryDirectories)
                {
                    var categoryName = Path.GetFileName(categoryDir);
                    if (Enum.TryParse<ServiceType>(categoryName, out var serviceType))
                    {
                        var serviceDirectories = await FileSystemManager.GetDirectoriesAsync(categoryDir);
                        foreach (var serviceDir in serviceDirectories)
                        {
                            var serviceName = Path.GetFileName(serviceDir);
                            var service = new ServiceModel
                            {
                                Name = serviceName,
                                DisplayName = GetServiceDisplayName(serviceName, serviceType),
                                Command = string.Empty,
                                Path = serviceDir,
                                ServiceType = serviceType,
                                Status = ServiceStatus.Stopped,
                                IsLoading = false,
                                IsSelected = false
                            };

                            var (command, workingDirectory) = await GetServiceCommandAsync(service, settings);
                            service.Command = command;
                            service.WorkingDirectory = workingDirectory;
                            service.IsSelected = IsServiceSelected(service, settings);
                            allServices.Add(service);
                        }
                    }
                }
                return allServices.AsReadOnly();
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing services");
                return Enumerable.Empty<ServiceModel>();
            }
        }

        public async Task<IEnumerable<ServiceDefinition>> GetAvailableServices()
        {
            var servicesFilePath = Path.Combine(PathManager.ConfigPath, "services.ini");
            if (!await FileSystemManager.FileExistsAsync(servicesFilePath))
            {
                return Enumerable.Empty<ServiceDefinition>();
            }
            var iniContent = await FileSystemManager.ReadAllTextAsync(servicesFilePath);
            var parser = new FileIniDataParser();
            var data = parser.Parser.Parse(iniContent);
            var allServices = new List<ServiceDefinition>();
            foreach (var section in data.Sections)
            {
                var categoryName = section.SectionName;
                var hasAdditionalDir = false;
                if (section.Keys.ContainsKey("has_additional_dir"))
                {
                    bool.TryParse(section.Keys["has_additional_dir"], out hasAdditionalDir);
                }
                if (!Enum.TryParse<ServiceType>(categoryName, out var serviceType))
                    continue;
                var serviceNames = new HashSet<string>();
                foreach (var key in section.Keys)
                {
                    if (key.KeyName.EndsWith(".name") || key.KeyName.EndsWith(".url"))
                    {
                        var serviceName = key.KeyName.Substring(0, key.KeyName.LastIndexOf('.'));
                        serviceNames.Add(serviceName);
                    }
                }
                foreach (var serviceName in serviceNames)
                {
                    var nameKey = $"{serviceName}.name";
                    var urlKey = $"{serviceName}.url";
                    if (section.Keys.ContainsKey(nameKey) && section.Keys.ContainsKey(urlKey))
                    {
                        var serviceDefinition = new ServiceDefinition
                        {
                            Name = serviceName,
                            Url = section.Keys[urlKey],
                            Description = section.Keys[nameKey],
                            ServiceType = serviceType,
                            HasAdditionalDir = hasAdditionalDir
                        };
                        allServices.Add(serviceDefinition);
                    }
                }
            }
            return allServices;
        }

        public async Task<bool> StartServiceAsync(ServiceModel service)
        {
            if (service == null) return false;

            try
            {
                service.IsLoading = true;
                service.Status = ServiceStatus.Starting;

                var workingDirectory = service.WorkingDirectory ?? Environment.CurrentDirectory;
                var process = await _commandManager.StartProcessAsync(service.Command, workingDirectory, default);

                if (process != null)
                {
                    process.EnableRaisingEvents = true;

                    process.Exited += (sender, e) =>
                    {
                        _uiDispatcher.TryEnqueue(() =>
                        {
                            service.Status = ServiceStatus.Stopped;
                            service.Process = null;
                        });
                    };

                    service.Process = process;
                    service.Status = ServiceStatus.Running;
                    return true;
                }
                else
                {
                    service.Status = ServiceStatus.Stopped;
                    return false;
                }
            }
            catch (Exception)
            {
                service.Status = ServiceStatus.Stopped;
                return false;
            }
            finally
            {
                service.IsLoading = false;
            }
        }

        public async Task<bool> StopServiceAsync(ServiceModel service)
        {
            if (service == null) return false;

            try
            {
                service.IsLoading = true;
                service.Status = ServiceStatus.Stopping;

                if (service.Process != null)
                {
                    var killTree = new ProcessStartInfo
                    {
                        FileName = "taskkill",
                        Arguments = $"/PID {service.Process.Id} /T /F",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    Process.Start(killTree)?.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to kill process tree: {ex.Message}");
            }
            finally
            {
                if (service.Process != null)
                {
                    service.Process.Dispose();
                    service.Process = null;
                }

                service.IsLoading = false;
            }

            service.Status = ServiceStatus.Stopped;

            await Task.CompletedTask;
            return true;
        }

        public async Task<bool> ToggleServiceAsync(ServiceModel service)
        {
            if (service == null)
            {
                return false;
            }

            if (service.IsRunning)
            {
                return await StopServiceAsync(service);
            }
            else
            {
                return await StartServiceAsync(service);
            }
        }

        private async Task<(string, string)> GetServiceCommandAsync(ServiceModel service, SettingsModel settings)
        {
            try
            {
                return service.ServiceType switch
                {
                    ServiceType.Apache => await ApacheSettingsService.GetCommandAsync(service, settings),
                    ServiceType.MySQL => await MySQLSettingsService.GetCommandAsync(service, settings),
                    ServiceType.Nginx => await NginxSettingsService.GetCommandAsync(service, settings),
                    ServiceType.Node => await NodeSettingsService.GetCommandAsync(service, settings),
                    ServiceType.Redis => await RedisSettingsService.GetCommandAsync(service, settings),
                    ServiceType.PostgreSQL => await PostgreSQLSettingsService.GetCommandAsync(service, settings),
                    ServiceType.MongoDB => await MongoDBSettingsService.GetCommandAsync(service, settings),
                    ServiceType.PHP => await PHPSettingsService.GetCommandAsync(service, settings),
                    _ => await Task.FromResult((string.Empty, string.Empty)),
                };
            }
            catch (Exception)
            {
                return (string.Empty, string.Empty);
            }
        }

        private static string GetServiceDisplayName(string serviceName, ServiceType serviceType)
        {
            return serviceType switch
            {
                ServiceType.Apache => "Apache HTTP Server",
                ServiceType.MySQL => "MySQL Database Server",
                ServiceType.PHP => "PHP FastCGI Process Manager",
                ServiceType.Nginx => "Nginx Web Server",
                ServiceType.Node => "Node.js Runtime",
                ServiceType.Redis => "Redis Server",
                ServiceType.PostgreSQL => "PostgreSQL Database Server",
                ServiceType.MongoDB => "MongoDB Database Server",
                _ => $"{serviceType} Service"
            };
        }

        private static bool IsServiceSelected(ServiceModel service, SettingsModel settings)
        {
            var selectedVersion = service.ServiceType switch
            {
                ServiceType.Apache => settings.Apache.Version,
                ServiceType.MySQL => settings.MySQL.Version,
                ServiceType.PHP => settings.PHP.Version,
                ServiceType.Nginx => settings.Nginx.Version,
                ServiceType.Node => settings.Node.Version,
                ServiceType.Redis => settings.Redis.Version,
                ServiceType.PostgreSQL => settings.PostgreSQL.Version,
                ServiceType.MongoDB => settings.MongoDB.Version,
                _ => string.Empty
            };

            return !string.IsNullOrEmpty(selectedVersion) && service.Name.Equals(selectedVersion, StringComparison.OrdinalIgnoreCase);
        }

        public Task<bool> IsServiceRunningAsync(ServiceModel service)
        {
            if (service == null) return Task.FromResult(false);

            return Task.FromResult(service.IsRunning);
        }
    }
}
