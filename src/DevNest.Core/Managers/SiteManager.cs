using DevNest.Core.Managers.Commands;
using DevNest.Core.Exceptions;
using DevNest.Core.Helpers;
using DevNest.Core.Interfaces;
using DevNest.Core.Managers.Sites;
using DevNest.Core.Models;
using IniParser.Parser;

namespace DevNest.Core
{
    public class SiteManager
    {
        private readonly VirtualHostManager _virtualHostManager;
        private readonly ICommandExecutor _commandExecutor;

        public SiteManager(VirtualHostManager virtualHostManager, ICommandExecutor commandExecutor)
        {
            _virtualHostManager = virtualHostManager;
            _commandExecutor = commandExecutor;
        }

        public async Task<IEnumerable<SiteModel>> GetInstalledSitesAsync()
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

        public async Task<IEnumerable<SiteDefinition>> GetAvailableSiteDefinitionsAsync()
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

        public async Task<SiteModel> InstallSiteAsync(string siteDefinitionName, string name, IProgress<string>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new SiteException(name, "Site name cannot be empty.");
            }

            if (await FileSystemHelper.DirectoryExistsAsync(Path.Combine(PathHelper.WwwPath, name)))
            {
                throw new SiteException(name, $"Site '{name}' already exists.");
            }

            var availableSiteDefinitions = await GetAvailableSiteDefinitionsAsync();
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
                    await _commandExecutor.ExecuteCommandAsync(actualCommand, workingDirectory, progress);
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
                    await _commandExecutor.ExecuteCommandAsync(actualCommand, workingDirectory, progress);
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
