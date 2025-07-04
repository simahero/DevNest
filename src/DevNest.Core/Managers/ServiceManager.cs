using DevNest.Core.Enums;
using DevNest.Core.Helpers;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using DevNest.Core.Services;
using DevNest.Core.State;
using IniParser;


namespace DevNest.Core
{
    public class ServiceManager
    {
        private readonly AppState _appState;
        private readonly IPlatformServiceFactory _platformServiceFactory;

        public ServiceManager(AppState appState, IPlatformServiceFactory platformServiceFactory)
        {
            _appState = appState;
            _platformServiceFactory = platformServiceFactory;
        }

        public async Task<IEnumerable<ServiceModel>> GetServicesAsync()
        {
            try
            {
                var allServices = new List<ServiceModel>();

                var settings = _appState.Settings;
                var binDirectory = PathHelper.BinPath;
                var categoryDirectories = await FileSystemHelper.GetDirectoriesAsync(binDirectory);

                foreach (var categoryDir in categoryDirectories)
                {
                    var categoryName = Path.GetFileName(categoryDir);
                    if (Enum.TryParse<ServiceType>(categoryName, out var serviceType))
                    {
                        var serviceDirectories = await FileSystemHelper.GetDirectoriesAsync(categoryDir);
                        foreach (var serviceDir in serviceDirectories)
                        {
                            var serviceName = Path.GetFileName(serviceDir);
                            var service = new ServiceModel
                            {
                                Name = serviceName,
                                DisplayName = GetServiceDisplayName(serviceType),
                                Command = string.Empty,
                                Path = serviceDir,
                                ServiceType = serviceType,
                                Status = ServiceStatus.Stopped,
                                IsLoading = false,
                                IsSelected = false
                            };

                            var (command, workingDirectory) = settings != null
                                ? await _platformServiceFactory.GetCommandManager().GetCommand(service, settings)
                                : (string.Empty, string.Empty);
                            service.Command = command;
                            service.WorkingDirectory = workingDirectory;
                            service.IsSelected = settings != null && IsServiceSelected(service, settings);
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
            var servicesFilePath = Path.Combine(PathHelper.ConfigPath, "services.ini");
            if (!await FileSystemHelper.FileExistsAsync(servicesFilePath))
            {
                return Enumerable.Empty<ServiceDefinition>();
            }
            var iniContent = await FileSystemHelper.ReadAllTextAsync(servicesFilePath);
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

        private static string GetServiceDisplayName(ServiceType serviceType)
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

    }
}
