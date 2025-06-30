using DevNest.Core.Commands;
using DevNest.Core.Exceptions;
using DevNest.Core.Files;
using DevNest.Core.Models;
using DevNest.Core.Sites;
using IniParser.Model;
using IniParser.Parser;

namespace DevNest.Core
{
    public class SiteManager
    {
        private readonly FileSystemManager _fileSystemManager;
        private readonly SettingsManager _settingsManager;
        private readonly VirtualHostManager _virtualHostManager;
        private readonly DownloadManager _downloadManager;
        private readonly ArchiveExtractionManager _archiveExtractionManager;
        private readonly CommandManager _commandManager;
        private readonly PathManager _pathManager;

        public SiteManager(
            FileSystemManager fileSystemManager,
            SettingsManager settingsManager,
            VirtualHostManager virtualHostManager,
            DownloadManager downloadManager,
            ArchiveExtractionManager archiveExtractionManager,
            CommandManager commandManager,
            PathManager pathManager)
        {
            _fileSystemManager = fileSystemManager;
            _settingsManager = settingsManager;
            _virtualHostManager = virtualHostManager;
            _downloadManager = downloadManager;
            _archiveExtractionManager = archiveExtractionManager;
            _commandManager = commandManager;
            _pathManager = pathManager;
        }

        public async Task<IEnumerable<SiteModel>> GetInstalledSitesAsync()
        {
            var sitesPath = _pathManager.WwwPath;

            if (!await _fileSystemManager.DirectoryExistsAsync(sitesPath))
            {
                return new List<SiteModel>();
            }

            var sites = new List<SiteModel>();
            var siteDirectories = await _fileSystemManager.GetDirectoriesAsync(sitesPath);

            foreach (var siteDir in siteDirectories)
            {
                var siteName = Path.GetFileName(siteDir);
                var site = new SiteModel
                {
                    Name = siteName,
                    Path = siteDir,
                    Url = $"http://{siteName}.test",
                    CreatedDate = Directory.GetCreationTime(siteDir),
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
                var sitesIniPath = Path.Combine(_pathManager.ConfigPath, "sites.ini");
                if (!await _fileSystemManager.FileExistsAsync(sitesIniPath))
                {
                    return new List<SiteDefinition>();
                }

                var content = await _fileSystemManager.ReadAllTextAsync(sitesIniPath);
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

            var sitesPath = _pathManager.WwwPath;
            var sitePath = Path.Combine(sitesPath, name);

            try
            {
                if (!await _fileSystemManager.DirectoryExistsAsync(sitesPath))
                {
                    await _fileSystemManager.CreateDirectoryAsync(sitesPath);
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
                await _fileSystemManager.DeleteDirectoryAsync(site.Path, true);
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
                    var tempFilePath = await _downloadManager.DownloadToTempAsync(siteDefinition.InstallUrl, progress);
                    await _archiveExtractionManager.ExtractAsync(tempFilePath, sitePath, siteDefinition.HasAdditionalDir, progress);

                    if (await _fileSystemManager.FileExistsAsync(tempFilePath))
                    {
                        await _fileSystemManager.DeleteFileAsync(tempFilePath);
                    }
                }
                return;
            }
        }

        public async Task OpenSiteAsync(string siteName)
        {
            var sites = await GetInstalledSitesAsync();
            var site = sites.FirstOrDefault(s => s.Name.Equals(siteName, StringComparison.OrdinalIgnoreCase));

            if (site == null)
                throw new SiteException(siteName, $"Site '{siteName}' not found.");

            try
            {
                await _commandManager.ExecuteCommandAsync($"cmd /c start \"{site.Url}\"", Environment.CurrentDirectory);
            }
            catch (Exception ex)
            {
                throw new SiteException(siteName, $"Failed to open site '{siteName}': {ex.Message}", ex);
            }
        }

        public async Task ExploreSiteAsync(string siteName)
        {
            var sites = await GetInstalledSitesAsync();
            var site = sites.FirstOrDefault(s => s.Name.Equals(siteName, StringComparison.OrdinalIgnoreCase));

            if (site == null)
                throw new SiteException(siteName, $"Site '{siteName}' not found.");

            try
            {
                await _commandManager.ExecuteCommandAsync($"explorer.exe \"{site.Path}\"", Environment.CurrentDirectory);
            }
            catch (Exception ex)
            {
                throw new SiteException(siteName, $"Failed to explore site '{siteName}': {ex.Message}", ex);
            }
        }

        public async Task OpenSiteInVSCodeAsync(string siteName)
        {
            var sites = await GetInstalledSitesAsync();
            var site = sites.FirstOrDefault(s => s.Name.Equals(siteName, StringComparison.OrdinalIgnoreCase));

            if (site == null)
                throw new SiteException(siteName, $"Site '{siteName}' not found.");

            try
            {
                await _commandManager.ExecuteCommandAsync($"code \"{site.Path}\"", Environment.CurrentDirectory);
            }
            catch (Exception ex)
            {
                throw new SiteException(siteName, $"Failed to open site '{siteName}' in VS Code: {ex.Message}", ex);
            }
        }

        public async Task OpenSiteInTerminalAsync(string siteName)
        {
            var sites = await GetInstalledSitesAsync();
            var site = sites.FirstOrDefault(s => s.Name.Equals(siteName, StringComparison.OrdinalIgnoreCase));

            if (site == null)
                throw new SiteException(siteName, $"Site '{siteName}' not found.");

            try
            {
                await _commandManager.ExecuteCommandAsync($"pwsh.exe -NoExit -Command \"cd '{site.Path}'\"", Environment.CurrentDirectory);
            }
            catch (Exception ex)
            {
                throw new SiteException(siteName, $"Failed to open terminal for site '{siteName}': {ex.Message}", ex);
            }
        }

        public async Task OpenSiteInBrowserAsync(string siteName)
        {
            var sites = await GetInstalledSitesAsync();
            var site = sites.FirstOrDefault(s => s.Name.Equals(siteName, StringComparison.OrdinalIgnoreCase));

            if (site == null)
                throw new SiteException(siteName, $"Site '{siteName}' not found.");

            try
            {
                await _commandManager.ExecuteCommandAsync($"cmd /c start \"\" \"{site.Url}\"", Environment.CurrentDirectory);
            }
            catch (Exception ex)
            {
                throw new SiteException(siteName, $"Failed to open site '{siteName}' in browser: {ex.Message}", ex);
            }
        }

    }
}
