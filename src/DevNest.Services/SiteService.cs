using DevNest.Core.Exceptions;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using System.Diagnostics;

namespace DevNest.Services
{
    public class SiteService : ISiteService
    {
        private readonly IFileSystemService _fileSystemService;
        private readonly IAppSettingsService _appSettingsService;
        private readonly ISitesReaderService _sitesReaderService;

        public SiteService(IFileSystemService fileSystemService, IAppSettingsService appSettingsService, ISitesReaderService sitesReaderService)
        {
            _fileSystemService = fileSystemService;
            _appSettingsService = appSettingsService;
            _sitesReaderService = sitesReaderService;
        }

        public async Task<IEnumerable<Site>> GetInstalledSitesAsync()
        {
            var settings = await _appSettingsService.LoadSettingsAsync();
            var sitesPath = Path.Combine(settings.InstallDirectory, "sites");

            if (!await _fileSystemService.DirectoryExistsAsync(sitesPath))
            {
                return new List<Site>();
            }

            var sites = new List<Site>();
            var siteDirectories = await _fileSystemService.GetDirectoriesAsync(sitesPath);

            foreach (var siteDir in siteDirectories)
            {
                var siteName = Path.GetFileName(siteDir);
                var site = new Site
                {
                    Name = siteName,
                    Path = siteDir,
                    Type = DetectSiteType(siteDir),
                    Url = $"http://{siteName}.local",
                    CreatedDate = Directory.GetCreationTime(siteDir),
                    IsActive = true
                };

                sites.Add(site);
            }

            return sites;
        }
        public async Task<IEnumerable<SiteType>> GetAvailableSiteTypesAsync()
        {
            return await _sitesReaderService.LoadSiteTypesAsync();
        }
        public async Task<Site> InstallSiteAsync(string siteTypeName, string name, IProgress<string>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new SiteException(name, "Site name cannot be empty.");

            if (await IsSiteInstalledAsync(name))
                throw new SiteException(name, $"Site '{name}' already exists.");

            // Get the site type from the available types
            var availableSiteTypes = await GetAvailableSiteTypesAsync();
            var siteType = availableSiteTypes.FirstOrDefault(st => st.Name.Equals(siteTypeName, StringComparison.OrdinalIgnoreCase));

            if (siteType == null)
                throw new SiteException(name, $"Site type '{siteTypeName}' not found.");

            var settings = await _appSettingsService.LoadSettingsAsync();
            var sitesPath = Path.Combine(settings.InstallDirectory, "sites");
            var sitePath = Path.Combine(sitesPath, name);

            try
            {
                if (!await _fileSystemService.DirectoryExistsAsync(sitesPath))
                {
                    await _fileSystemService.CreateDirectoryAsync(sitesPath);
                }

                await _fileSystemService.CreateDirectoryAsync(sitePath);

                await CreateSiteStructureAsync(siteType, sitePath, progress);

                var site = new Site
                {
                    Name = name,
                    Path = sitePath,
                    Type = siteType.Name,
                    Url = $"http://{name}.local",
                    CreatedDate = DateTime.Now,
                    IsActive = true
                };

                await CreateVirtualHostIfEnabledAsync(name, progress);

                return site;
            }
            catch (Exception ex)
            {
                throw new SiteException(name, $"Failed to install site '{name}': {ex.Message}", ex);
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
                throw new SiteException(siteName, "Site name cannot be empty.");

            var sites = await GetInstalledSitesAsync();
            var site = sites.FirstOrDefault(s => s.Name.Equals(siteName, StringComparison.OrdinalIgnoreCase));

            if (site == null)
                throw new SiteException(siteName, $"Site '{siteName}' not found.");

            try
            {
                await _fileSystemService.DeleteDirectoryAsync(site.Path, true);
            }
            catch (Exception ex)
            {
                throw new SiteException(siteName, $"Failed to remove site '{siteName}': {ex.Message}", ex);
            }
        }
        private async Task CreateSiteStructureAsync(SiteType siteType, string sitePath, IProgress<string>? progress = null)
        {
            if (siteType.InstallType.ToLower() == "none")
            {
                // No installation required for this site type
                return;
            }

            if (siteType.InstallType.ToLower() == "command")
            {
                if (!string.IsNullOrEmpty(siteType.Command))
                {
                    await RunInstallCommandAsync(siteType.Command, Path.GetFileName(sitePath), sitePath, progress);
                }
                return;
            }

            if (siteType.InstallType.ToLower() == "download")
            {
                if (!string.IsNullOrEmpty(siteType.Url))
                {
                    var tempFilePath = await DownloadToTempAsync(siteType.Url, progress);
                    await ExtractAsync(tempFilePath, sitePath, siteType.HasAdditionalDir, progress);

                    // Clean up temp file
                    if (await _fileSystemService.FileExistsAsync(tempFilePath))
                    {
                        await _fileSystemService.DeleteFileAsync(tempFilePath);
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
                var startInfo = new ProcessStartInfo
                {
                    FileName = site.Url,
                    UseShellExecute = true
                };
                Process.Start(startInfo);
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
                var startInfo = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = site.Path,
                    UseShellExecute = true
                };
                Process.Start(startInfo);
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
                var startInfo = new ProcessStartInfo
                {
                    FileName = "code",
                    Arguments = $"\"{site.Path}\"",
                    UseShellExecute = true
                };
                Process.Start(startInfo);
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
                var startInfo = new ProcessStartInfo
                {
                    FileName = "pwsh.exe",
                    Arguments = $"-NoExit -Command \"cd '{site.Path}'\"",
                    UseShellExecute = true
                };
                Process.Start(startInfo);
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
                // Try to detect if there's a development server running
                var url = await DetectSiteUrlAsync(site);

                var startInfo = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                throw new SiteException(siteName, $"Failed to open site '{siteName}' in browser: {ex.Message}", ex);
            }
        }



        private string DetectSiteType(string sitePath)
        {
            // Check for various framework indicators
            if (File.Exists(Path.Combine(sitePath, "package.json")))
                return "Node.js";
            if (File.Exists(Path.Combine(sitePath, "composer.json")))
                return "PHP";
            if (File.Exists(Path.Combine(sitePath, "index.html")))
                return "Static HTML";
            if (Directory.Exists(Path.Combine(sitePath, "wp-content")))
                return "WordPress";

            return "Unknown";
        }
        private async Task<string> DetectSiteUrlAsync(Site site)
        {
            // Check for common development server configurations
            var packageJsonPath = Path.Combine(site.Path, "package.json");

            if (await _fileSystemService.FileExistsAsync(packageJsonPath))
            {
                // Check if it's a React, Vue, or other Node.js app
                var packageContent = await _fileSystemService.ReadAllTextAsync(packageJsonPath);

                if (packageContent.Contains("react-scripts") || packageContent.Contains("create-react-app"))
                {
                    return "http://localhost:3000"; // Default React dev server
                }
                else if (packageContent.Contains("vue") || packageContent.Contains("@vue/cli"))
                {
                    return "http://localhost:8080"; // Default Vue dev server
                }
                else if (packageContent.Contains("next"))
                {
                    return "http://localhost:3000"; // Default Next.js dev server
                }
                else if (packageContent.Contains("express"))
                {
                    return "http://localhost:3000"; // Default Express server
                }
            }

            // Check for Laravel
            var laravelPath = Path.Combine(site.Path, "artisan");
            if (await _fileSystemService.FileExistsAsync(laravelPath))
            {
                return "http://localhost:8000"; // Default Laravel dev server
            }

            // Check for PHP files
            var phpFiles = Directory.GetFiles(site.Path, "*.php", SearchOption.TopDirectoryOnly);
            if (phpFiles.Length > 0)
            {
                return "http://localhost"; // Generic PHP server
            }

            // Check for static HTML
            var indexHtml = Path.Combine(site.Path, "index.html");
            if (await _fileSystemService.FileExistsAsync(indexHtml))
            {
                return $"file:///{site.Path.Replace('\\', '/')}/index.html";
            }

            // Default fallback
            return site.Url ?? $"http://{site.Name}.local";
        }

        private async Task<string> DownloadToTempAsync(string url, IProgress<string>? progress = null)
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(10);

            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");

            var fileName = GetFileNameFromUrl(url);

            progress?.Report($"Downloading from: {url}...");

            HttpResponseMessage response;
            try
            {
                response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? url;
                if (finalUrl.Contains("error") || finalUrl.Contains("404") || finalUrl.Contains("not-found"))
                {
                    throw new Exception($"Download URL appears to be invalid or file not found: {finalUrl}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to download from {url}: {ex.Message}");
            }

            if (response.Content.Headers.ContentDisposition?.FileName != null)
            {
                fileName = response.Content.Headers.ContentDisposition.FileName.Trim('"');
            }

            var contentBytes = await response.Content.ReadAsByteArrayAsync();

            if (contentBytes.Length < 100)
            {
                throw new Exception("Downloaded file is too small - likely an error page or redirect");
            }

            var contentStart = System.Text.Encoding.UTF8.GetString(contentBytes.Take(100).ToArray()).ToLower();
            if (contentStart.Contains("<html") ||
                contentStart.Contains("<!doctype") ||
                contentStart.Contains("<title>") ||
                contentStart.Contains("error") ||
                contentStart.Contains("not found") ||
                contentStart.Contains("404"))
            {
                throw new Exception("Downloaded content appears to be an HTML error page instead of the expected file");
            }

            var sizeInMB = contentBytes.Length / 1024.0 / 1024.0;
            progress?.Report($"Downloading {fileName} ({sizeInMB:F1} MB)");

            var tempFilePath = Path.Combine(Path.GetTempPath(), fileName);
            await File.WriteAllBytesAsync(tempFilePath, contentBytes);

            return tempFilePath;
        }

        private string GetFileNameFromUrl(string url)
        {
            var uri = new Uri(url);
            var fileName = Path.GetFileName(uri.LocalPath);

            if (string.IsNullOrEmpty(fileName) || !fileName.Contains('.'))
            {
                fileName = $"download_{DateTime.Now:yyyyMMddHHmmss}.zip";
            }

            return fileName;
        }

        private async Task ExtractAsync(string filePath, string destination, bool hasAdditionalDir, IProgress<string>? progress = null)
        {
            var fileName = Path.GetFileName(filePath);
            progress?.Report($"Extracting {fileName}...");

            if (IsArchive(filePath))
            {
                await ExtractZipFileAsync(filePath, destination, hasAdditionalDir, progress);
            }
            else
            {
                if (!await _fileSystemService.DirectoryExistsAsync(destination))
                {
                    await _fileSystemService.CreateDirectoryAsync(destination);
                }

                var destinationFile = Path.Combine(destination, fileName);
                await _fileSystemService.CopyFileAsync(filePath, destinationFile);
                progress?.Report($"Copied {fileName} to {destination}");
            }
        }

        private static readonly string[] ArchiveExts = [".zip", ".rar", ".7z", ".tar", ".tar.gz", ".tgz", ".tar.bz2", ".tbz2"];

        private static bool IsArchive(string path)
        {
            string fileName = Path.GetFileName(path);
            return ArchiveExts.Any(e => fileName.EndsWith(e, StringComparison.OrdinalIgnoreCase));
        }

        private async Task ExtractZipFileAsync(string filePath, string destination, bool hasAdditionalDir, IProgress<string>? progress = null)
        {
            var tempExtractPath = Path.Combine(Path.GetTempPath(), $"devnest_extract_{Guid.NewGuid()}");

            try
            {
                await Task.Run(() =>
                {
                    System.IO.Compression.ZipFile.ExtractToDirectory(filePath, tempExtractPath);

                    var extractedDirs = Directory.GetDirectories(tempExtractPath);
                    var extractedFiles = Directory.GetFiles(tempExtractPath);

                    if (!Directory.Exists(destination))
                    {
                        Directory.CreateDirectory(destination);
                    }

                    if (hasAdditionalDir && extractedDirs.Length > 0)
                    {
                        var sourceDir = extractedDirs[0];
                        var subDirs = Directory.GetDirectories(sourceDir);
                        var subFiles = Directory.GetFiles(sourceDir);

                        progress?.Report("Moving extracted contents...");

                        foreach (var dir in subDirs)
                        {
                            var dirName = Path.GetFileName(dir);
                            var targetDir = Path.Combine(destination, dirName);
                            if (Directory.Exists(targetDir))
                            {
                                Directory.Delete(targetDir, true);
                            }
                            Directory.Move(dir, targetDir);
                        }

                        foreach (var file in subFiles)
                        {
                            var fileName = Path.GetFileName(file);
                            var targetFile = Path.Combine(destination, fileName);
                            if (File.Exists(targetFile))
                            {
                                File.Delete(targetFile);
                            }
                            File.Move(file, targetFile);
                        }
                    }
                    else
                    {
                        progress?.Report("Moving extracted files...");

                        foreach (var dir in extractedDirs)
                        {
                            var dirName = Path.GetFileName(dir);
                            var targetDir = Path.Combine(destination, dirName);
                            if (Directory.Exists(targetDir))
                            {
                                Directory.Delete(targetDir, true);
                            }
                            Directory.Move(dir, targetDir);
                        }

                        foreach (var file in extractedFiles)
                        {
                            var fileName = Path.GetFileName(file);
                            var targetFile = Path.Combine(destination, fileName);
                            if (File.Exists(targetFile))
                            {
                                File.Delete(targetFile);
                            }
                            File.Move(file, targetFile);
                        }
                    }
                });

                progress?.Report("Extraction completed successfully");
            }
            catch (InvalidDataException ex)
            {
                throw new Exception($"Archive appears to be corrupted: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Extraction failed: {ex.Message}");
            }
            finally
            {
                if (Directory.Exists(tempExtractPath))
                {
                    Directory.Delete(tempExtractPath, true);
                }
            }
        }

        private async Task RunInstallCommandAsync(string command, string siteName, string sitePath, IProgress<string>? progress = null)
        {
            string actualCommand = command.Replace("%s", siteName);

            progress?.Report($"Running: {actualCommand}");

            var processInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c cd /d \"{Path.GetDirectoryName(sitePath)}\" && {actualCommand}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            progress?.Report("Starting installation process...");

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                progress?.Report("Installing dependencies and setting up project...");

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    string error = await process.StandardError.ReadToEndAsync();
                    throw new Exception($"Command failed with exit code {process.ExitCode}: {error}");
                }

                progress?.Report("Installation completed successfully!");
            }
        }

        private async Task CreateVirtualHostIfEnabledAsync(string siteName, IProgress<string>? progress = null)
        {
            var settings = await _appSettingsService.LoadSettingsAsync();
            if (settings.AutoVirtualHosts)
            {
                try
                {
                    progress?.Report("Creating virtual host...");
                    await CreateVirtualHostAsync(siteName);
                }
                catch (Exception ex)
                {
                    progress?.Report($"Virtual host creation failed for {siteName}: {ex.Message}");
                }
            }
            else
            {
                progress?.Report("Creating virtual host disabled.");
            }
        }

        private async Task CreateVirtualHostAsync(string siteName)
        {
            var domain = $"{siteName}.dev";
            var documentRoot = $"C:/DevNest/www/{siteName}";

            // Add virtual host to Apache configuration
            await AddApacheVirtualHostAsync(siteName, domain, documentRoot);

            // Add entry to hosts file
            await AddHostsEntryAsync(domain);
        }

        private async Task AddApacheVirtualHostAsync(string siteName, string domain, string documentRoot)
        {
            var vhostConfig = await ProcessVirtualHostTemplateAsync(siteName, domain, documentRoot);

            var sitesEnabledPath = @"C:\DevNest\etc\apache\sites-enabled";

            // Ensure the sites-enabled directory exists
            if (!await _fileSystemService.DirectoryExistsAsync(sitesEnabledPath))
            {
                await _fileSystemService.CreateDirectoryAsync(sitesEnabledPath);
            }

            var configFilePath = Path.Combine(sitesEnabledPath, $"auto.{domain}.conf");

            // Check if the virtual host configuration already exists
            if (await _fileSystemService.FileExistsAsync(configFilePath))
            {
                return; // Virtual host already exists
            }

            // Create the individual virtual host configuration file
            await _fileSystemService.WriteAllTextAsync(configFilePath, vhostConfig);
        }

        private async Task<string> ProcessVirtualHostTemplateAsync(string siteName, string domain, string documentRoot)
        {
            var templatePath = @"C:\DevNest\usr\tpl\VirtualHost.tpl";

            try
            {
                // Read the template file
                var templateContent = await _fileSystemService.ReadAllTextAsync(templatePath);

                // Replace template placeholders
                var processedContent = templateContent
                    .Replace("<<PORT>>", "80")
                    .Replace("<<PROJECT_DIR>>", documentRoot)
                    .Replace("<<HOSTNAME>>", domain)
                    .Replace("<<SITENAME>>", siteName);

                return processedContent;

            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to process template: {ex.Message}");
            }
        }

        private async Task AddHostsEntryAsync(string domain)
        {
            var hostsFilePath = @"C:\Windows\System32\drivers\etc\hosts";
            var hostsEntry = $"127.0.0.1\t{domain}\t#DevNest";

            // Check if hosts file exists and if entry already exists
            if (await _fileSystemService.FileExistsAsync(hostsFilePath))
            {
                var existingContent = await _fileSystemService.ReadAllTextAsync(hostsFilePath);
                if (existingContent.Contains(domain))
                {
                    return;
                }
            }

            try
            {
                // Try to append to hosts file directly
                var currentContent = await _fileSystemService.ReadAllTextAsync(hostsFilePath);
                var newContent = currentContent + Environment.NewLine + hostsEntry;
                await _fileSystemService.WriteAllTextAsync(hostsFilePath, newContent);
            }
            catch (Exception)
            {
                // If direct access fails, try using PowerShell with elevated privileges
                await AddHostsEntryViaPowerShellAsync(domain, hostsEntry);
            }
        }

        private async Task AddHostsEntryViaPowerShellAsync(string domain, string hostsEntry)
        {
            var hostsFilePath = @"C:\Windows\System32\drivers\etc\hosts";

            try
            {
                var escapedEntry = hostsEntry.Replace("'", "''"); // Escape single quotes for PowerShell
                var powershellCommand = $"Add-Content -Path '{hostsFilePath}' -Value '{escapedEntry}'";

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"Start-Process powershell -ArgumentList '-Command', \\\"{powershellCommand}\\\" -Verb RunAs -Wait\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = Process.Start(processStartInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                    {
                        var error = await process.StandardError.ReadToEndAsync();
                        throw new Exception($"Failed to add hosts entry via PowerShell: {error}");
                    }
                }
                else
                {
                    throw new Exception("Failed to start PowerShell process");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Administrator privileges required to modify hosts file. Please manually add: {hostsEntry}. Error: {ex.Message}");
            }
        }
    }
}
