using DevNest.Core.Interfaces;
using System.Diagnostics;

namespace DevNest.Services.Sites
{
    public class VirtualHostService : IVirtualHostService
    {
        private readonly IFileSystemService _fileSystemService;
        private readonly IPathService _pathService;
        private readonly SettingsManager _settingsManager;

        public VirtualHostService(IFileSystemService fileSystemService, IPathService pathService, SettingsManager settingsManager)
        {
            _fileSystemService = fileSystemService;
            _pathService = pathService;
            _settingsManager = settingsManager;
        }

        public async Task CreateVirtualHostAsync(string siteName, IProgress<string>? progress = null)
        {
            var settings = await _settingsManager.LoadSettingsAsync();
            if (!settings.AutoVirtualHosts)
            {
                progress?.Report("Virtual host creation disabled.");
                return;
            }

            try
            {
                progress?.Report("Creating virtual host...");

                var domain = $"{siteName}.test";
                var documentRoot = Path.Combine(_pathService.WwwPath, siteName);

                await AddApacheVirtualHostAsync(siteName, domain, documentRoot);

                await AddHostsEntryAsync(domain);

                progress?.Report("Virtual host created successfully.");
            }
            catch (Exception ex)
            {
                progress?.Report($"Virtual host creation failed for {siteName}: {ex.Message}");
                throw;
            }
        }

        private async Task AddApacheVirtualHostAsync(string siteName, string domain, string documentRoot)
        {
            var vhostConfig = await ProcessVirtualHostTemplateAsync(siteName, domain, documentRoot);

            var sitesEnabledPath = _pathService.SitesEnabledPath;

            if (!await _fileSystemService.DirectoryExistsAsync(sitesEnabledPath))
            {
                await _fileSystemService.CreateDirectoryAsync(sitesEnabledPath);
            }

            var configFilePath = Path.Combine(sitesEnabledPath, $"auto.{domain}.conf");

            if (await _fileSystemService.FileExistsAsync(configFilePath))
            {
                return;
            }

            await _fileSystemService.WriteAllTextAsync(configFilePath, vhostConfig);
        }

        private async Task<string> ProcessVirtualHostTemplateAsync(string siteName, string domain, string documentRoot)
        {
            var templatePath = @"C:\DevNest\template\auto.virtualhost.conf.tpl";

            try
            {
                var templateContent = await _fileSystemService.ReadAllTextAsync(templatePath);

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
                var currentContent = await _fileSystemService.ReadAllTextAsync(hostsFilePath);
                var newContent = currentContent + Environment.NewLine + hostsEntry;
                await _fileSystemService.WriteAllTextAsync(hostsFilePath, newContent);
            }
            catch (Exception)
            {
                await AddHostsEntryViaPowerShellAsync(domain, hostsEntry);
            }
        }

        private async Task AddHostsEntryViaPowerShellAsync(string domain, string hostsEntry)
        {
            var hostsFilePath = @"C:\Windows\System32\drivers\etc\hosts";

            try
            {
                var escapedEntry = hostsEntry.Replace("'", "''");
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
