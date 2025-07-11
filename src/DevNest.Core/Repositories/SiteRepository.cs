using DevNest.Core.Exceptions;
using DevNest.Core.Helpers;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using DevNest.Core.Services;
using IniParser.Parser;

namespace DevNest.Core.Repositories
{

    public class SiteRepository : ISiteRepository
    {
        private readonly PlatformServiceFactory _platformServiceFactory;

        public SiteRepository(PlatformServiceFactory platformServiceFactory)
        {
            _platformServiceFactory = platformServiceFactory;
        }

        public async Task<IEnumerable<SiteModel>> GetSitesAsync()
        {
            var sitesPath = PathHelper.WwwPath;

            if (!await FileSystemHelper.DirectoryExistsAsync(sitesPath))
            {
                return new List<SiteModel>();
            }

            var sites = new List<SiteModel>();
            var siteDirectories = await FileSystemHelper.GetDirectoriesAsync(sitesPath);

            foreach (var siteDir in siteDirectories)
            {
                var siteName = Path.GetFileName(siteDir);
                var site = new SiteModel
                {
                    Name = siteName,
                    Path = siteDir,
                    Url = $"http://{siteName}.test",
                    IsActive = true
                };

                sites.Add(site);
            }

            return sites;
        }

        public async Task<IEnumerable<SiteDefinition>> GetAvailableSitesAsync()
        {
            try
            {
                var sitesIniPath = Path.Combine(PathHelper.ConfigPath, "sites.ini");
                if (!await FileSystemHelper.FileExistsAsync(sitesIniPath))
                {
                    return new List<SiteDefinition>();
                }

                var content = await FileSystemHelper.ReadAllTextAsync(sitesIniPath);
                var iniData = new IniDataParser().Parse(content);
                var siteDefinitions = new List<SiteDefinition>();

                if (iniData.Sections.ContainsSection("SiteTypes"))
                {
                    var siteDefinitionsSection = iniData.Sections["SiteTypes"];
                    var siteDefinitionGroups = siteDefinitionsSection.GroupBy(key => key.KeyName.Split('.')[0]);

                    foreach (var group in siteDefinitionGroups)
                    {
                        var siteDefinition = new SiteDefinition();
                        foreach (var key in group)
                        {
                            var property = key.KeyName.Split('.')[1];
                            switch (property)
                            {
                                case "Name":
                                    siteDefinition.Name = key.Value ?? string.Empty;
                                    break;
                                case "InstallType":
                                    siteDefinition.InstallType = key.Value ?? string.Empty;
                                    break;
                                case "InstallUrl":
                                    siteDefinition.InstallUrl = key.Value ?? string.Empty;
                                    break;
                                case "InstallCommand":
                                    siteDefinition.InstallCommand = key.Value ?? string.Empty;
                                    break;
                                case "HasAdditionalDir":
                                    siteDefinition.HasAdditionalDir = bool.Parse(key.Value ?? "false");
                                    break;
                            }
                        }
                        siteDefinitions.Add(siteDefinition);
                    }
                }

                return siteDefinitions;
            }
            catch (Exception ex)
            {
                var errorMessage = $"Failed to load site types: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(errorMessage);
                throw new Exception(errorMessage, ex);
            }
        }

        public async Task<SiteModel> CreateSiteAsync(string siteDefinitionName, string name, IProgress<string>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new SiteException(name, "Site name cannot be empty.");
            }

            if (await SiteExistsAsync(name))
            {
                throw new SiteException(name, $"Site '{name}' already exists.");
            }

            var availableSiteDefinitions = await GetAvailableSitesAsync();
            var siteDefinition = availableSiteDefinitions.FirstOrDefault(st => st.Name.Equals(siteDefinitionName, StringComparison.OrdinalIgnoreCase));

            if (siteDefinition == null)
            {
                throw new SiteException(name, $"Site type '{siteDefinitionName}' not found.");
            }

            var sitesPath = PathHelper.WwwPath;
            var sitePath = Path.Combine(sitesPath, name);

            try
            {
                if (!await FileSystemHelper.DirectoryExistsAsync(sitesPath))
                {
                    await FileSystemHelper.CreateDirectoryAsync(sitesPath);
                }

                await CreateSiteStructureAsync(siteDefinition, sitePath, progress);

                var site = new SiteModel
                {
                    Name = name,
                    Path = sitePath,
                    Url = $"http://{name}.test",
                    IsActive = true
                };

                IVirtualHostManager _virtualHostManager = _platformServiceFactory.GetVirtualHostManager();
                await _virtualHostManager.CreateVirtualHostAsync(name, progress);

                return site;
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error occurred in SiteRepository: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(errorMessage);
                throw new SiteException("SiteRepository", errorMessage, ex);
            }
        }

        public async Task DeleteSiteAsync(string siteName)
        {
            var sitePath = Path.Combine(PathHelper.WwwPath, siteName);
            if (await FileSystemHelper.DirectoryExistsAsync(sitePath))
            {
                await FileSystemHelper.DeleteDirectoryAsync(sitePath, recursive: true);
            }
        }

        public async Task<bool> SiteExistsAsync(string siteName)
        {
            var sitePath = Path.Combine(PathHelper.WwwPath, siteName);
            return await FileSystemHelper.DirectoryExistsAsync(sitePath);
        }

        private async Task CreateSiteStructureAsync(SiteDefinition siteDefinition, string sitePath, IProgress<string>? progress = null)
        {
            if (siteDefinition.InstallType.ToLower() == "none")
            {
                if (!await FileSystemHelper.DirectoryExistsAsync(sitePath))
                {
                    await FileSystemHelper.CreateDirectoryAsync(sitePath);
                }
                return;
            }

            if (siteDefinition.InstallType.ToLower() == "clone")
            {
                if (!string.IsNullOrEmpty(siteDefinition.InstallCommand))
                {
                    string actualCommand = siteDefinition.InstallCommand.Replace("%s", Path.GetFileName(sitePath));
                    string workingDirectory = Path.GetDirectoryName(PathHelper.WwwPath) ?? throw new ArgumentException("Invalid site path", nameof(sitePath));

                    progress?.Report("Installing dependencies and setting up project...");
                    var commandExecutor = _platformServiceFactory.GetCommandExecutor();
                    await commandExecutor.ExecuteCommandAsync(actualCommand, workingDirectory, progress);
                    progress?.Report("Installation completed successfully!");
                }
                return;
            }

            if (siteDefinition.InstallType.ToLower() == "command")
            {
                if (!string.IsNullOrEmpty(siteDefinition.InstallCommand))
                {
                    string actualCommand = siteDefinition.InstallCommand.Replace("%s", Path.GetFileName(sitePath));
                    string workingDirectory = Path.GetDirectoryName(sitePath) ?? throw new ArgumentException("Invalid site path", nameof(sitePath));

                    progress?.Report("Installing dependencies and setting up project...");
                    var commandExecutor = _platformServiceFactory.GetCommandExecutor();
                    await commandExecutor.ExecuteCommandAsync(actualCommand, workingDirectory, progress);
                    progress?.Report("Installation completed successfully!");
                }
                return;
            }

            if (siteDefinition.InstallType.ToLower() == "download")
            {
                if (!string.IsNullOrEmpty(siteDefinition.InstallUrl))
                {
                    progress?.Report("Downloading and extracting files...");
                    var tempFilePath = await DownloadHelper.DownloadToTempAsync(siteDefinition.InstallUrl, progress);
                    await ArchiveHelper.ExtractAsync(tempFilePath, sitePath, siteDefinition.HasAdditionalDir, progress);

                    if (await FileSystemHelper.FileExistsAsync(tempFilePath))
                    {
                        await FileSystemHelper.DeleteFileAsync(tempFilePath);
                    }
                }
                return;
            }
        }

    }
}
