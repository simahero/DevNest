using DevNest.Core.Enums;
using DevNest.Core.Models;
using System.IO.Compression;
using DevNest.Core.Helpers;


namespace DevNest.Core
{
    public class InstallManager
    {
        private readonly SettingsManager _settingsManager;
        private readonly HttpClient _httpClient;

        public InstallManager(SettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(10);

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

                var settings = await _settingsManager.LoadSettingsAsync();
                var servicesDir = Path.Combine(PathManager.BinPath, serviceDefinition.ServiceType.ToString());
                var serviceDir = Path.Combine(servicesDir, serviceDefinition.Name);

                if (await IsServiceInstalledAsync(serviceDefinition.Name))
                {
                    return new InstallationResultModel
                    {
                        Success = false,
                        Message = $"Service '{serviceDefinition.Name}' is already installed"
                    };
                }

                progress?.Report("Creating installation directory...");
                await FileSystemManager.CreateDirectoryAsync(serviceDir);

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

                progress?.Report("Extracting files...");
                await ExtractServiceAsync(downloadPath, serviceDir, serviceDefinition.HasAdditionalDir, progress);

                if (await FileSystemManager.FileExistsAsync(downloadPath))
                {
                    await FileSystemManager.DeleteFileAsync(downloadPath);
                }

                object? serviceSettings = serviceDefinition.ServiceType switch
                {
                    ServiceType.Apache => settings.Apache,
                    ServiceType.MySQL => settings.MySQL,
                    ServiceType.PHP => settings.PHP,
                    ServiceType.Node => settings.Node,
                    ServiceType.Redis => settings.Redis,
                    ServiceType.PostgreSQL => settings.PostgreSQL,
                    ServiceType.Nginx => settings.Nginx,
                    _ => null
                };

                if (serviceSettings != null)
                {
                    var versionProp = serviceSettings.GetType().GetProperty("Version");
                    var versionValue = versionProp?.GetValue(serviceSettings) as string;
                    if (string.IsNullOrEmpty(versionValue))
                    {
                        versionProp?.SetValue(serviceSettings, serviceDefinition.Name);
                        _ = LogManager.Log($"No version set yet, setting a default version: {serviceDefinition.Name}");
                    }
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
                var serviceDir = Path.Combine(PathManager.BinPath, serviceName);
                return await FileSystemManager.DirectoryExistsAsync(serviceDir);
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
                var serviceDir = Path.Combine(PathManager.BinPath, serviceName);

                if (!await FileSystemManager.DirectoryExistsAsync(serviceDir))
                {
                    return new InstallationResultModel
                    {
                        Success = false,
                        Message = $"Service '{serviceName}' is not installed"
                    };
                }

                await FileSystemManager.DeleteDirectoryAsync(serviceDir, true);

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
                await FileSystemManager.WriteAllBytesAsync(tempPath, contentBytes);

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
                        await FileSystemManager.CreateDirectoryAsync(destinationDir);
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
                await FileSystemManager.CopyFileAsync(archivePath, destinationFile);
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
