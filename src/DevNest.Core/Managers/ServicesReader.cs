using DevNest.Core.Files;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using IniParser;

namespace DevNest.Core
{
    public class ServicesReader : IServicesReader
    {
        private readonly FileSystemManager _fileSystemManager;
        private readonly PathManager _pathManager;

        public ServicesReader(FileSystemManager fileSystemManager, PathManager pathManager)
        {
            _fileSystemManager = fileSystemManager;
            _pathManager = pathManager;
        }

        public async Task<List<ServiceModel>> LoadInstalledServicesAsync()
        {
            try
            {
                var binDirectory = _pathManager.BinPath;

                var allServices = new List<ServiceModel>();
                var categoryDirectories = await _fileSystemManager.GetDirectoriesAsync(binDirectory);

                foreach (var categoryDir in categoryDirectories)
                {
                    var categoryName = Path.GetFileName(categoryDir);
                    var serviceDirectories = await _fileSystemManager.GetDirectoriesAsync(categoryDir);

                    foreach (var serviceDir in serviceDirectories)
                    {
                        var serviceName = Path.GetFileName(serviceDir);
                        var service = new ServiceModel
                        {
                            Name = serviceName,
                            DisplayName = GetServiceDisplayName(serviceName, categoryName),
                            Command = string.Empty, // Will be set by ServiceManager
                            Path = serviceDir,
                            ServiceType = categoryName,
                            IsSelected = false // Will be updated later with settings
                        };

                        allServices.Add(service);
                    }
                }

                return allServices;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading installed services: {ex.Message}");
                return new List<ServiceModel>();
            }
        }

        public async Task<List<ServiceModel>> LoadInstalledServicesAsync(SettingsModel settings)
        {
            var services = await LoadInstalledServicesAsync();

            // Update IsSelected based on settings
            foreach (var service in services)
            {
                service.IsSelected = IsServiceSelectedBySettings(service, settings);
            }

            return services;
        }

        private static bool IsServiceSelectedBySettings(ServiceModel service, SettingsModel settings)
        {
            var selectedVersion = service.ServiceType.ToLowerInvariant() switch
            {
                "apache" => settings.Apache.Version,
                "mysql" => settings.MySQL.Version,
                "php" => settings.PHP.Version,
                "nginx" => settings.Nginx.Version,
                "node" => settings.Node.Version,
                "redis" => settings.Redis.Version,
                "postgresql" => settings.PostgreSQL.Version,
                "mongodb" => settings.MongoDB.Version,
                _ => string.Empty
            };

            return !string.IsNullOrEmpty(selectedVersion) &&
                   service.Name.Equals(selectedVersion, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetServiceDisplayName(string serviceName, string serviceType)
        {
            return serviceType switch
            {
                "Apache" => "Apache HTTP Server",
                "MySQL" => "MySQL Database Server",
                "PHP" => "PHP FastCGI Process Manager",
                "Nginx" => "Nginx Web Server",
                "Node" => "Node.js Runtime",
                "Redis" => "Redis Server",
                "PostgreSQL" => "PostgreSQL Database Server",
                "MongoDB" => "MongoDB Database Server",
                _ => $"{serviceType} Service"
            };
        }

        public async Task<List<ServiceDefinition>> LoadAvailableServicesAsync()
        {
            try
            {
                var servicesFilePath = Path.Combine(_pathManager.ConfigPath, "services.ini");

                if (!await _fileSystemManager.FileExistsAsync(servicesFilePath))
                {
                    return new List<ServiceDefinition>();
                }

                var iniContent = await _fileSystemManager.ReadAllTextAsync(servicesFilePath);
                var parser = new FileIniDataParser();
                var data = parser.Parser.Parse(iniContent);

                var allServices = new List<ServiceDefinition>();

                foreach (var section in data.Sections)
                {
                    var categoryName = section.SectionName;
                    var hasAdditionalDir = false;

                    // Check if category has has_additional_dir property
                    if (section.Keys.ContainsKey("has_additional_dir"))
                    {
                        bool.TryParse(section.Keys["has_additional_dir"], out hasAdditionalDir);
                    }

                    // Find all service entries in this section
                    var serviceNames = new HashSet<string>();
                    foreach (var key in section.Keys)
                    {
                        if (key.KeyName.EndsWith(".name") || key.KeyName.EndsWith(".url"))
                        {
                            var serviceName = key.KeyName.Substring(0, key.KeyName.LastIndexOf('.'));
                            serviceNames.Add(serviceName);
                        }
                    }

                    // Create ServiceDefinition objects for each service
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
                                ServiceType = categoryName,
                                HasAdditionalDir = hasAdditionalDir
                            };
                            allServices.Add(serviceDefinition);
                        }
                    }
                }

                return allServices;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading available services: {ex.Message}");
                return new List<ServiceDefinition>();
            }
        }
    }
}
