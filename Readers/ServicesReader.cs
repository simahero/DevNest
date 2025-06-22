using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DevNest.Readers
{
    public class ServicesReader
    {
        private readonly string _installDirectory;
        private readonly string _servicesFilePath;

        public ServicesReader(string installDirectory = @"C:\DevNest\bin", string servicesFilePath = @"C:\DevNest\services.json")
        {
            _installDirectory = installDirectory;
            _servicesFilePath = servicesFilePath;
        }

        public List<InstalledService> LoadInstalledServices()
        {
            try
            {
                if (!Directory.Exists(_installDirectory))
                {
                    Directory.CreateDirectory(_installDirectory);
                }

                var allServices = new List<InstalledService>();

                var categoryDirectories = Directory.GetDirectories(_installDirectory);

                foreach (var categoryDir in categoryDirectories)
                {
                    var categoryName = Path.GetFileName(categoryDir);
                    var serviceDirectories = Directory.GetDirectories(categoryDir);

                    foreach (var serviceDir in serviceDirectories)
                    {
                        var serviceName = Path.GetFileName(serviceDir);
                        var service = new InstalledService
                        {
                            Name = serviceName,
                            Path = serviceDir,
                            Category = categoryName
                        };

                        allServices.Add(service);
                    }
                }

                return allServices;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error loading installed services: {ex.Message}", ex);
            }
        }

        public async Task<List<ServiceItem>> LoadServiceConfigurationAsync()
        {
            try
            {
                if (!File.Exists(_servicesFilePath))
                {
                    throw new FileNotFoundException($"services.json file not found at {_servicesFilePath}");
                }

                var jsonContent = await File.ReadAllTextAsync(_servicesFilePath);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var servicesConfig = JsonSerializer.Deserialize<ServicesConfiguration>(jsonContent, options);

                if (servicesConfig?.Categories == null)
                {
                    throw new InvalidOperationException("Invalid services.json format");
                }

                var services = new List<ServiceItem>();
                foreach (var category in servicesConfig.Categories)
                {
                    if (category.Services != null)
                    {
                        foreach (var service in category.Services)
                        {
                            services.Add(new ServiceItem
                            {
                                Name = service.Name,
                                Url = service.Url,
                                Description = service.Description,
                                Category = category.Name,
                                HasAdditionalDir = category.HasAdditionalDir
                            });
                        }
                    }
                }

                return services;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error loading service configuration: {ex.Message}", ex);
            }
        }

    }
    public class InstalledService
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    public class ServiceItem
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool HasAdditionalDir { get; set; } = false;
        public string DisplayName => $"{Name} - {Description}";
    }

    public class ServicesConfiguration
    {
        public List<ServiceCategory> Categories { get; set; } = new();
    }

    public class ServiceCategory
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("has_additional_dir")]
        public bool HasAdditionalDir { get; set; } = false;

        public List<ServiceDefinition> Services { get; set; } = new();
    }

    public class ServiceDefinition
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }
}
