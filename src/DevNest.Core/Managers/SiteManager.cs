using DevNest.Core.Commands;
using DevNest.Core.Exceptions;
using DevNest.Core.Helpers;
using DevNest.Core.Models;
using DevNest.Core.Sites;
using IniParser.Model;
using IniParser.Parser;

namespace DevNest.Core
{
    public class SiteManager
    {
        private readonly SettingsManager _settingsManager;
        private readonly VirtualHostManager _virtualHostManager;
        private readonly CommandManager _commandManager;

        public SiteManager(SettingsManager settingsManager, VirtualHostManager virtualHostManager, CommandManager commandManager)
        {
            _settingsManager = settingsManager;
            _virtualHostManager = virtualHostManager;
            _commandManager = commandManager;
        }

        public async Task<IEnumerable<SiteModel>> GetInstalledSitesAsync()
        {
            var sitesPath = PathManager.WwwPath;

            if (!await FileSystemManager.DirectoryExistsAsync(sitesPath))
            {
                return new List<SiteModel>();
            }

            var sites = new List<SiteModel>();
            var siteDirectories = await FileSystemManager.GetDirectoriesAsync(sitesPath);

            foreach (var siteDir in siteDirectories)
            {
                var siteName = Path.GetFileName(siteDir);
                var createdDate = await FileSystemManager.GetDirectoryCreationTimeAsync(siteDir);
                var site = new SiteModel
                {
                    Name = siteName,
                    Path = siteDir,
                    Url = $"http://{siteName}.test",
                    CreatedDate = createdDate,
                    IsActive = true
                };

                sites.Add(site);
            }

            return sites;
        }

        public async Task<IEnumerable<SiteDefinition>> GetAvailableSiteDefinitionsAsync()
        {
            try
            {
                var sitesIniPath = Path.Combine(PathManager.ConfigPath, "sites.ini");
                if (!await FileSystemManager.FileExistsAsync(sitesIniPath))
                {
                    return new List<SiteDefinition>();
                }

                var content = await FileSystemManager.ReadAllTextAsync(sitesIniPath);
                var iniData = new IniDataParser().Parse(content);
                var siteDefinitions = ParseIniToSiteDefinitions(iniData);

                return siteDefinitions;
            }
            catch (Exception ex)
            {
                var errorMessage = $"Failed to load site types: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(errorMessage);
                throw new Exception(errorMessage, ex);
            }
        }

        private IEnumerable<SiteDefinition> ParseIniToSiteDefinitions(IniData iniData)
        {
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

        public async Task<SiteModel> InstallSiteAsync(string siteDefinitionName, string name, IProgress<string>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new SiteException(name, "Site name cannot be empty.");
            }

            if (await IsSiteInstalledAsync(name))
            {
                throw new SiteException(name, $"Site '{name}' already exists.");
            }

            var availableSiteDefinitions = await GetAvailableSiteDefinitionsAsync();
            var siteDefinition = availableSiteDefinitions.FirstOrDefault(st => st.Name.Equals(siteDefinitionName, StringComparison.OrdinalIgnoreCase));

            if (siteDefinition == null)
            {
                throw new SiteException(name, $"Site type '{siteDefinitionName}' not found.");
            }

            var sitesPath = PathManager.WwwPath;
            var sitePath = Path.Combine(sitesPath, name);

            try
            {
                if (!await FileSystemManager.DirectoryExistsAsync(sitesPath))
                {
                    await FileSystemManager.CreateDirectoryAsync(sitesPath);
                }

                await CreateSiteStructureAsync(siteDefinition, sitePath, progress);

                var site = new SiteModel
                {
                    Name = name,
                    Path = sitePath,
                    Url = $"http://{name}.test",
                    CreatedDate = DateTime.Now,
                    IsActive = true
                };

                await _virtualHostManager.CreateVirtualHostAsync(name, progress);

                return site;
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error occurred in SiteService: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(errorMessage);
                throw new SiteException("SiteService", errorMessage, ex);
            }
        }

        public async Task<bool> IsSiteInstalledAsync(string siteName)
        {
            var sites = await GetInstalledSitesAsync();
            return sites.Any(s => s.Name.Equals(siteName, StringComparison.OrdinalIgnoreCase));
        }

        public async Task RemoveSiteAsync(string siteName)
        {
            if (string.IsNullOrWhiteSpace(siteName))
            {
                throw new SiteException(siteName, "Site name cannot be empty.");
            }

            var sites = await GetInstalledSitesAsync();
            var site = sites.FirstOrDefault(s => s.Name.Equals(siteName, StringComparison.OrdinalIgnoreCase));

            if (site == null)
            {
                throw new SiteException(siteName, $"Site '{siteName}' not found.");
            }

            try
            {
                await FileSystemManager.DeleteDirectoryAsync(site.Path, true);
            }
            catch (Exception ex)
            {
                throw new SiteException(siteName, $"Failed to remove site '{siteName}': {ex.Message}", ex);
            }
        }

        private async Task CreateSiteStructureAsync(SiteDefinition siteDefinition, string sitePath, IProgress<string>? progress = null)
        {
            if (siteDefinition.InstallType.ToLower() == "none")
            {
                if (!await FileSystemManager.DirectoryExistsAsync(sitePath))
                {
                    await FileSystemManager.CreateDirectoryAsync(sitePath);
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
                    await _commandManager.ExecuteCommandWithSuccessCheckAsync(actualCommand, workingDirectory, progress);
                    progress?.Report("Installation completed successfully!");
                }
                return;
            }

            if (siteDefinition.InstallType.ToLower() == "download")
            {
                if (!string.IsNullOrEmpty(siteDefinition.InstallUrl))
                {
                    var tempFilePath = await DownloadHelper.DownloadToTempAsync(siteDefinition.InstallUrl, progress);
                    await ArchiveHelper.ExtractAsync(tempFilePath, sitePath, siteDefinition.HasAdditionalDir, progress);

                    if (await FileSystemManager.FileExistsAsync(tempFilePath))
                    {
                        await FileSystemManager.DeleteFileAsync(tempFilePath);
                    }
                }
                return;
            }
        }
    }
}
