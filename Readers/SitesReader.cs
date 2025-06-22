using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DevNest.Readers
{
    public class SitesReader
    {
        private readonly string _wwwPath;
        private readonly string _siteConfigPath;

        public SitesReader(string wwwPath = @"C:\DevNest\www", string siteConfigPath = @"C:\DevNest\sites.json")
        {
            _wwwPath = wwwPath;
            _siteConfigPath = siteConfigPath;
        }
        public List<InstalledSite> LoadSites()
        {
            try
            {
                if (!Directory.Exists(_wwwPath))
                {
                    Directory.CreateDirectory(_wwwPath);
                }

                var allSites = new List<InstalledSite>();

                var siteDirectories = Directory.GetDirectories(_wwwPath);

                foreach (var siteDir in siteDirectories)
                {
                    var siteName = Path.GetFileName(siteDir);
                    var site = new InstalledSite
                    {
                        Name = siteName,
                        Path = siteDir,
                    };

                    allSites.Add(site);
                }

                return allSites;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error loading installed sites: {ex.Message}", ex);
            }
        }

        public async Task<List<SiteType>> LoadSiteConfiguration()
        {
            if (!File.Exists(_siteConfigPath))
            {
                throw new FileNotFoundException($"Site configuration file not found at {_siteConfigPath}");
            }

            var jsonContent = await File.ReadAllTextAsync(_siteConfigPath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var sitesConfig = JsonSerializer.Deserialize<SiteConfiguration>(jsonContent, options);

            if (sitesConfig?.types == null)
            {
                throw new InvalidOperationException("Invalid services.json format");
            }

            var sites = new List<SiteType>();
            foreach (var type in sitesConfig.types)
            {
                sites.Add(new SiteType
                {
                    Name = type.Name,
                    InstallType = type.InstallType,
                    Url = type.Url,
                    Command = type.Command,
                    HasAdditionalDir = type.HasAdditionalDir
                });
            }

            return sites;

        }
        public class InstalledSite
        {
            public string Name { get; set; } = string.Empty;
            public string Path { get; set; } = string.Empty;
        }

        public class SiteConfiguration
        {
            public bool AutoCreateDatabase { get; set; }
            public bool Cached { get; set; }
            public List<SiteType> types { get; set; } = new List<SiteType>();
        }

        public class SiteType
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("install_type")]
            public string InstallType { get; set; } = string.Empty;

            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonPropertyName("command")]
            public string? Command { get; set; }

            [JsonPropertyName("has_additional_dir")]
            public bool HasAdditionalDir { get; set; } = false;
        }
    }
}
