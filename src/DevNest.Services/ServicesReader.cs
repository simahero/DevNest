using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using IniParser;

namespace DevNest.Services
{
    public class ServicesReader : IServicesReader
    {
        private readonly string _installDirectory;
        private readonly string _servicesFilePath;
        private readonly IFileSystemService _fileSystemService; public ServicesReader(IFileSystemService fileSystemService, string installDirectory = @"C:\DevNest\bin", string servicesFilePath = @"C:\DevNest\services.ini")
        {
            _fileSystemService = fileSystemService;
            _installDirectory = installDirectory;
            _servicesFilePath = servicesFilePath;
        }

        public async Task<List<InstalledService>> LoadInstalledServicesAsync()
        {
            try
            {
                if (!await _fileSystemService.DirectoryExistsAsync(_installDirectory))
                {
                    await _fileSystemService.CreateDirectoryAsync(_installDirectory);
                }

                var allServices = new List<InstalledService>();
                var categoryDirectories = await _fileSystemService.GetDirectoriesAsync(_installDirectory); foreach (var categoryDir in categoryDirectories)
                {
                    var categoryName = Path.GetFileName(categoryDir);
                    var serviceDirectories = await _fileSystemService.GetDirectoriesAsync(categoryDir);

                    foreach (var serviceDir in serviceDirectories)
                    {
                        var serviceName = Path.GetFileName(serviceDir);
                        var service = new InstalledService
                        {
                            Name = serviceName,
                            Path = serviceDir,
                            ServiceType = categoryName
                        };

                        allServices.Add(service);
                    }
                }

                return allServices;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading installed services: {ex.Message}");
                return new List<InstalledService>();
            }
        }
        public async Task<List<ServiceDefinition>> LoadAvailableServicesAsync()
        {
            try
            {
                if (!await _fileSystemService.FileExistsAsync(_servicesFilePath))
                {
                    return new List<ServiceDefinition>();
                }

                var iniContent = await _fileSystemService.ReadAllTextAsync(_servicesFilePath);
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
