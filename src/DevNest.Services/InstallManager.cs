using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using System.IO.Compression;

namespace DevNest.Services
{
    public class InstallManager
    {
        private readonly IFileSystemService _fileSystemService;
        private readonly IPathService _pathService;
        private readonly SettingsManager _settingsManager;
        private readonly HttpClient _httpClient;

        public InstallManager(
            IFileSystemService fileSystemService,
            IPathService pathService,
            SettingsManager settingsManager)
        {
            _fileSystemService = fileSystemService;
            _pathService = pathService;
            _settingsManager = settingsManager;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(10);

            // Set headers similar to DownloadController
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        }

        public async Task<InstallationResultModel> InstallServiceAsync(ServiceDefinition serviceDefinition, IProgress<string>? progress = null)
        {
            try
            {
                if (serviceDefinition == null)
                {
                    return new InstallationResultModel
                    {
                        Success = false,
                        Message = "Service definition is required"
                    };
                }

                progress?.Report($"Starting installation of {serviceDefinition.Name}...");

                // Get installation directory from settings
                var settings = await _settingsManager.LoadSettingsAsync();
                var servicesDir = Path.Combine(_pathService.BinPath, serviceDefinition.ServiceType);
                var serviceDir = Path.Combine(servicesDir, serviceDefinition.Name);

                // Check if already installed
                if (await IsServiceInstalledAsync(serviceDefinition.Name))
                {
                    return new InstallationResultModel
                    {
                        Success = false,
                        Message = $"Service '{serviceDefinition.Name}' is already installed"
                    };
                }

                // Create service directory
                progress?.Report("Creating installation directory...");
                await _fileSystemService.CreateDirectoryAsync(serviceDir);

                // Download the service
                progress?.Report($"Downloading {serviceDefinition.Name} from {serviceDefinition.Url}...");
                var downloadPath = await DownloadServiceAsync(serviceDefinition.Url, progress);

                if (string.IsNullOrEmpty(downloadPath))
                {
                    return new InstallationResultModel
                    {
                        Success = false,
                        Message = "Failed to download service"
                    };
                }

                // Extract if it's a zip file
                progress?.Report("Extracting files...");
                await ExtractServiceAsync(downloadPath, serviceDir, serviceDefinition.HasAdditionalDir, progress);

                // Clean up downloaded file
                if (await _fileSystemService.FileExistsAsync(downloadPath))
                {
                    await _fileSystemService.DeleteFileAsync(downloadPath);
                }

                progress?.Report($"Successfully installed {serviceDefinition.Name}");

                return new InstallationResultModel
                {
                    Success = true,
                    Message = $"Service '{serviceDefinition.Name}' installed successfully",
                    InstalledPath = serviceDir
                };
            }
            catch (Exception ex)
            {
                return new InstallationResultModel
                {
                    Success = false,
                    Message = $"Installation failed: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<bool> IsServiceInstalledAsync(string serviceName)
        {
            try
            {
                var settings = await _settingsManager.LoadSettingsAsync();
                var serviceDir = Path.Combine(_pathService.BinPath, serviceName);
                return await _fileSystemService.DirectoryExistsAsync(serviceDir);
            }
            catch
            {
                return false;
            }
        }

        public async Task<InstallationResultModel> UninstallServiceAsync(string serviceName, IProgress<string>? progress = null)
        {
            try
            {
                progress?.Report($"Uninstalling {serviceName}...");

                var settings = await _settingsManager.LoadSettingsAsync();
                var serviceDir = Path.Combine(_pathService.BinPath, serviceName);

                if (!await _fileSystemService.DirectoryExistsAsync(serviceDir))
                {
                    return new InstallationResultModel
                    {
                        Success = false,
                        Message = $"Service '{serviceName}' is not installed"
                    };
                }

                await _fileSystemService.DeleteDirectoryAsync(serviceDir);

                progress?.Report($"Successfully uninstalled {serviceName}");

                return new InstallationResultModel
                {
                    Success = true,
                    Message = $"Service '{serviceName}' uninstalled successfully"
                };
            }
            catch (Exception ex)
            {
                return new InstallationResultModel
                {
                    Success = false,
                    Message = $"Uninstallation failed: {ex.Message}",
                    Exception = ex
                };
            }
        }

        private async Task<string?> DownloadServiceAsync(string url, IProgress<string>? progress = null)
        {
            try
            {
                progress?.Report($"Connecting to {url}...");

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                // Determine filename
                var fileName = GetFileNameFromUrl(url);
                if (response.Content.Headers.ContentDisposition?.FileName != null)
                {
                    fileName = response.Content.Headers.ContentDisposition.FileName.Trim('"');
                }

                // Save to temp directory
                var tempPath = Path.Combine(Path.GetTempPath(), fileName);
                var contentBytes = await response.Content.ReadAsByteArrayAsync();

                progress?.Report($"Saving to {tempPath}...");
                await File.WriteAllBytesAsync(tempPath, contentBytes);

                return tempPath;
            }
            catch (Exception ex)
            {
                progress?.Report($"Download failed: {ex.Message}");
                return null;
            }
        }

        private async Task ExtractServiceAsync(string archivePath, string destinationPath, bool hasAdditionalDir, IProgress<string>? progress = null)
        {
            if (Path.GetExtension(archivePath).ToLowerInvariant() == ".zip")
            {
                progress?.Report("Extracting ZIP archive...");

                using var archive = ZipFile.OpenRead(archivePath);

                foreach (var entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue; // Skip directories

                    var destinationFile = Path.Combine(destinationPath, entry.FullName);

                    // Handle additional directory structure
                    if (hasAdditionalDir)
                    {
                        var pathParts = entry.FullName.Split('/', '\\');
                        if (pathParts.Length > 1)
                        {
                            // Skip the first directory level
                            var relativePath = string.Join(Path.DirectorySeparatorChar, pathParts.Skip(1));
                            destinationFile = Path.Combine(destinationPath, relativePath);
                        }
                    }

                    var destinationDir = Path.GetDirectoryName(destinationFile);
                    if (!string.IsNullOrEmpty(destinationDir))
                    {
                        await _fileSystemService.CreateDirectoryAsync(destinationDir);
                    }

                    entry.ExtractToFile(destinationFile, overwrite: true);
                }
            }
            else
            {
                // For non-zip files, just copy the file
                progress?.Report("Copying file...");
                var fileName = Path.GetFileName(archivePath);
                var destinationFile = Path.Combine(destinationPath, fileName);
                await _fileSystemService.CopyFileAsync(archivePath, destinationFile);
            }
        }

        private static string GetFileNameFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var fileName = Path.GetFileName(uri.LocalPath);
                return string.IsNullOrEmpty(fileName) ? "download" : fileName;
            }
            catch
            {
                return "download";
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
