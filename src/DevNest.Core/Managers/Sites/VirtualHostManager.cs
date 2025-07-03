using DevNest.Core.Helpers;
using System.Diagnostics;

namespace DevNest.Core.Managers.Sites
{
    public class VirtualHostManager
    {
        private readonly SettingsManager _settingsManager;

        public VirtualHostManager(SettingsManager settingsManager)
        {
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
                var documentRoot = Path.Combine(PathHelper.WwwPath, siteName);

                await AddVirtualHostAsync(siteName, domain, documentRoot);

                await AddHostsEntryAsync(domain);

                progress?.Report("Virtual host created successfully.");
            }
            catch (Exception ex)
            {
                progress?.Report($"Virtual host creation failed for {siteName}: {ex.Message}");
                throw;
            }
        }

        private async Task AddVirtualHostAsync(string siteName, string domain, string documentRoot)
        {
            var apacheConfig = await ProcessApacheSitesEnabledTemplateAsync(siteName, domain, documentRoot);

            var apacheSitesEnabledPath = Path.Combine(PathHelper.EtcPath, "apache2", "sites-enabled");

            if (!await FileSystemHelper.DirectoryExistsAsync(apacheSitesEnabledPath))
            {
                await FileSystemHelper.CreateDirectoryAsync(apacheSitesEnabledPath);
            }

            var apacheConfigFilePath = Path.Combine(apacheSitesEnabledPath, $"auto.{domain}.conf");

            if (!await FileSystemHelper.FileExistsAsync(apacheConfigFilePath))
            {
                await FileSystemHelper.WriteAllTextAsync(apacheConfigFilePath, apacheConfig);
            }


            var nginxConfig = await ProcessNginxSitesEnabledTemplateAsync(siteName, domain, documentRoot);

            var nginxSitesEnabledPath = Path.Combine(PathHelper.EtcPath, "nginx", "sites-enabled");

            if (!await FileSystemHelper.DirectoryExistsAsync(nginxSitesEnabledPath))
            {
                await FileSystemHelper.CreateDirectoryAsync(nginxSitesEnabledPath);
            }

            var nginxConfigFilePath = Path.Combine(nginxSitesEnabledPath, $"auto.{domain}.conf");

            if (!await FileSystemHelper.FileExistsAsync(nginxConfigFilePath))
            {
                await FileSystemHelper.WriteAllTextAsync(nginxConfigFilePath, nginxConfig);
            }

        }

        private async Task<string> ProcessApacheSitesEnabledTemplateAsync(string siteName, string domain, string documentRoot)
        {
            var apacheTemplatePath = Path.Combine(PathHelper.TemplatesPath, "auto.apache.sites-enabled.conf.tpl");

            try
            {
                var templateContent = await FileSystemHelper.ReadAllTextAsync(apacheTemplatePath);

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

        private async Task<string> ProcessNginxSitesEnabledTemplateAsync(string siteName, string domain, string documentRoot)
        {
            var nginxTemplatePath = Path.Combine(PathHelper.TemplatesPath, "auto.nginx.sites-enabled.conf.tpl");

            try
            {
                var templateContent = await FileSystemHelper.ReadAllTextAsync(nginxTemplatePath);

                var processedContent = templateContent
                    .Replace("<<PORT>>", "8080")
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
            var hostsFilePath = @"C:\\Windows\\System32\\drivers\\etc\\hosts";
            var hostsEntry = $"127.0.0.1\t{domain}\t#DevNest";

            if (await FileSystemHelper.FileExistsAsync(hostsFilePath))
            {
                var existingContent = await FileSystemHelper.ReadAllTextAsync(hostsFilePath);
                if (existingContent.Contains(domain))
                {
                    return;
                }
            }

            try
            {
                var currentContent = await FileSystemHelper.ReadAllTextAsync(hostsFilePath);
                var newContent = currentContent + Environment.NewLine + hostsEntry;
                await FileSystemHelper.WriteAllTextAsync(hostsFilePath, newContent);
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
