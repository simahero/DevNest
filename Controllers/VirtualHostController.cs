using System;
using System.IO;
using System.Threading.Tasks;

namespace DevNest.Controllers
{
    public class VirtualHostController
    {
        private static readonly string SitesEnabledPath = @"C:\DevNest\etc\apache\sites-enabled";
        private static readonly string HostsFilePath = @"C:\Windows\System32\drivers\etc\hosts";
        private static readonly string TemplatePath = @"C:\DevNest\usr\tpl\VirtualHost.tpl";

        public async Task CreateVirtualHostAsync(string siteName)
        {
            try
            {
                var domain = $"{siteName}.dev";
                var documentRoot = $"C:/DevNest/www/{siteName}";

                // Add virtual host to Apache configuration
                await AddApacheVirtualHostAsync(siteName, domain, documentRoot);

                // Add entry to hosts file
                await AddHostsEntryAsync(domain);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create virtual host for {siteName}: {ex.Message}");
            }
        }
        private async Task AddApacheVirtualHostAsync(string siteName, string domain, string documentRoot)
        {
            // Read and process the template file
            var vhostConfig = await ProcessTemplateAsync(siteName, domain, documentRoot);

            // Ensure the sites-enabled directory exists
            if (!Directory.Exists(SitesEnabledPath))
            {
                Directory.CreateDirectory(SitesEnabledPath);
            }

            var configFilePath = Path.Combine(SitesEnabledPath, $"auto.{domain}.conf");

            // Check if the virtual host configuration already exists
            if (File.Exists(configFilePath))
            {
                return; // Virtual host already exists
            }

            // Create the individual virtual host configuration file
            await File.WriteAllTextAsync(configFilePath, vhostConfig);
        }

        private async Task<string> ProcessTemplateAsync(string siteName, string domain, string documentRoot)
        {
            try
            {
                // Read the template file
                var templateContent = await File.ReadAllTextAsync(TemplatePath);

                // Replace template placeholders
                var processedContent = templateContent
                    .Replace("<<PORT>>", "80")
                    .Replace("<<PROJECT_DIR>>", documentRoot)
                    .Replace("<<HOSTNAME>>", domain)
                    .Replace("<<SITENAME>>", siteName);

                return processedContent;
            }
            catch (FileNotFoundException)
            {
                throw new Exception($"Template file not found: {TemplatePath}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to process template: {ex.Message}");
            }
        }
        private async Task AddHostsEntryAsync(string domain)
        {
            var hostsEntry = $"127.0.0.1\t{domain}\t#DevNest";

            // Check if hosts file exists and if entry already exists
            if (File.Exists(HostsFilePath))
            {
                var existingContent = await File.ReadAllTextAsync(HostsFilePath);
                if (existingContent.Contains(domain))
                {
                    return;
                }
            }

            try
            {
                await File.AppendAllTextAsync(HostsFilePath, Environment.NewLine + hostsEntry);
            }
            catch (UnauthorizedAccessException)
            {
                // Try using PowerShell with elevated privileges
                await AddHostsEntryViaPowerShellAsync(domain, hostsEntry);
            }
        }

        private async Task AddHostsEntryViaPowerShellAsync(string domain, string hostsEntry)
        {
            try
            {
                var escapedEntry = hostsEntry.Replace("'", "''"); // Escape single quotes for PowerShell
                var powershellCommand = $"Add-Content -Path '{HostsFilePath}' -Value '{escapedEntry}'";

                var processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"Start-Process powershell -ArgumentList '-Command', \\\"{powershellCommand}\\\" -Verb RunAs -Wait\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = System.Diagnostics.Process.Start(processStartInfo);
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

        public async Task RemoveVirtualHostAsync(string siteName)
        {
            try
            {
                var domain = $"{siteName}.dev";

                // Remove from Apache configuration
                await RemoveApacheVirtualHostAsync(siteName, domain);

                // Remove from hosts file
                await RemoveHostsEntryAsync(domain);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to remove virtual host for {siteName}: {ex.Message}");
            }
        }
        private async Task RemoveApacheVirtualHostAsync(string siteName, string domain)
        {
            var configFilePath = Path.Combine(SitesEnabledPath, $"auto.{domain}.conf");

            if (File.Exists(configFilePath))
            {
                try
                {
                    File.Delete(configFilePath);
                    await Task.CompletedTask; // Keep async signature for consistency
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to remove virtual host configuration file: {ex.Message}");
                }
            }
        }
        private async Task RemoveHostsEntryAsync(string domain)
        {
            if (!File.Exists(HostsFilePath))
            {
                return;
            }

            try
            {
                var content = await File.ReadAllTextAsync(HostsFilePath);
                var lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                var result = new System.Text.StringBuilder();
                foreach (var line in lines)
                {
                    if (!line.Contains(domain))
                    {
                        result.AppendLine(line);
                    }
                }

                await File.WriteAllTextAsync(HostsFilePath, result.ToString().TrimEnd());
            }
            catch (UnauthorizedAccessException)
            {
                // Try using PowerShell with elevated privileges
                await RemoveHostsEntryViaPowerShellAsync(domain);
            }
        }

        private async Task RemoveHostsEntryViaPowerShellAsync(string domain)
        {
            try
            {
                var powershellCommand = $"(Get-Content '{HostsFilePath}') | Where-Object {{$_ -notmatch '{domain}'}} | Set-Content '{HostsFilePath}'";

                var processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"Start-Process powershell -ArgumentList '-Command', \\\"{powershellCommand}\\\" -Verb RunAs -Wait\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = System.Diagnostics.Process.Start(processStartInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                    {
                        var error = await process.StandardError.ReadToEndAsync();
                        throw new Exception($"Failed to remove hosts entry via PowerShell: {error}");
                    }
                }
                else
                {
                    throw new Exception("Failed to start PowerShell process");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Administrator privileges required to modify hosts file. Please manually remove entry for: {domain}. Error: {ex.Message}");
            }
        }
    }
}
