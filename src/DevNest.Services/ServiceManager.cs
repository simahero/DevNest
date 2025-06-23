using DevNest.Core.Exceptions;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using System.Diagnostics;

namespace DevNest.Services
{
    public class ServiceManager : IServiceManager
    {
        private static readonly string DevNestBinPath = @"C:\DevNest\bin";
        private readonly IServicesReader _servicesReader;
        private readonly IAppSettingsService _appSettingsService;
        private readonly IFileSystemService _fileSystemService;
        private readonly List<Service> _services;

        public ServiceManager(
            IServicesReader servicesReader,
            IAppSettingsService appSettingsService,
            IFileSystemService fileSystemService)
        {
            _servicesReader = servicesReader;
            _appSettingsService = appSettingsService;
            _fileSystemService = fileSystemService;
            _services = new List<Service>();
        }

        public async Task<IEnumerable<Service>> GetServicesAsync()
        {
            await RefreshServicesAsync();
            return _services.AsReadOnly();
        }

        public async Task<Service?> GetServiceAsync(string name)
        {
            await RefreshServicesAsync();
            return _services.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public async Task StartServiceAsync(string serviceName)
        {
            var service = await GetServiceAsync(serviceName);
            if (service == null)
                throw new ServiceException(serviceName, $"Service '{serviceName}' not found.");

            if (service.IsLoading || service.IsRunning)
                return;

            service.IsLoading = true;
            service.Status = ServiceStatus.Starting;

            try
            {
                var success = await StartServiceInternalAsync(service);
                if (!success)
                {
                    service.Status = ServiceStatus.Stopped;
                    throw new ServiceException(serviceName, $"Failed to start service '{serviceName}'.");
                }
            }
            catch (Exception ex)
            {
                service.Status = ServiceStatus.Stopped;
                throw new ServiceException(serviceName, $"Error starting service '{serviceName}': {ex.Message}", ex);
            }
            finally
            {
                service.IsLoading = false;
            }
        }

        public async Task StopServiceAsync(string serviceName)
        {
            var service = await GetServiceAsync(serviceName);
            if (service == null)
                throw new ServiceException(serviceName, $"Service '{serviceName}' not found.");

            if (service.IsLoading || !service.IsRunning)
                return;

            service.IsLoading = true;
            service.Status = ServiceStatus.Stopping;

            try
            {
                var success = await StopServiceInternalAsync(service);
                if (!success)
                {
                    throw new ServiceException(serviceName, $"Failed to stop service '{serviceName}'.");
                }
                service.Status = ServiceStatus.Stopped;
            }
            catch (Exception ex)
            {
                throw new ServiceException(serviceName, $"Error stopping service '{serviceName}': {ex.Message}", ex);
            }
            finally
            {
                service.IsLoading = false;
            }
        }

        public async Task<bool> IsServiceRunningAsync(string serviceName)
        {
            var service = await GetServiceAsync(serviceName);
            return service?.IsRunning ?? false;
        }

        public async Task RefreshServicesAsync()
        {
            if (!await _fileSystemService.DirectoryExistsAsync(DevNestBinPath))
            {
                await _fileSystemService.CreateDirectoryAsync(DevNestBinPath);
            }

            _services.Clear();

            var installedServices = await _servicesReader.LoadInstalledServicesAsync();

            foreach (var installedService in installedServices)
            {
                var command = await GetServiceCommandAsync(installedService);

                if (!string.IsNullOrEmpty(command))
                {
                    var service = new Service
                    {
                        Name = installedService.Name,
                        DisplayName = $"{installedService.ServiceType} - {installedService.Name}",
                        Command = command,
                        Status = ServiceStatus.Stopped
                    };

                    _services.Add(service);
                }
            }
        }

        private async Task<bool> StartServiceInternalAsync(Service service)
        {
            return await Task.Run(() =>
            {
                var command = service.Command;

                if (string.IsNullOrEmpty(command))
                {
                    Debug.WriteLine($"No command found for service: {service.Name}");
                    return false;
                }

                try
                {
                    ProcessStartInfo processStartInfo = CreateProcessStartInfo(command);
                    Debug.WriteLine($"Starting service {service.Name} with command: {command}");

                    var process = Process.Start(processStartInfo);
                    if (process != null)
                    {
                        service.Process = process;
                        service.Status = ServiceStatus.Running;

                        // Monitor the process
                        MonitorProcess(service, process);

                        Debug.WriteLine($"Service {service.Name} started successfully with PID: {process.Id}");
                        return true;
                    }

                    Debug.WriteLine($"Failed to start process for service {service.Name}");
                    return false;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error starting service {service.Name}: {ex.Message}");
                    return false;
                }
            });
        }

        private async Task<bool> StopServiceInternalAsync(Service service)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (service.Process != null && !service.Process.HasExited)
                    {
                        service.Process.Kill();
                        service.Process.WaitForExit(5000); // Wait up to 5 seconds
                        service.Process = null;
                        Debug.WriteLine($"Service {service.Name} stopped successfully");
                        return true;
                    }
                    return true; // Already stopped
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error stopping service {service.Name}: {ex.Message}");
                    return false;
                }
            });
        }

        private ProcessStartInfo CreateProcessStartInfo(string command)
        {
            // Check if command is a batch file
            if (command.EndsWith(".bat") && File.Exists(command))
            {
                return new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{command}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
            }
            // Check if command contains cmd.exe wrapper or directory change (cd /d)
            else if (command.Contains("cmd.exe /c") || command.Contains("cd /d"))
            {
                string cmdArgs;
                if (command.StartsWith("cmd.exe /c"))
                {
                    cmdArgs = command.Substring("cmd.exe /c".Length).Trim();
                }
                else
                {
                    cmdArgs = $"/c {command}";
                }

                return new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = cmdArgs,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
            }
            else
            {
                // Parse command and arguments
                var parts = ParseCommand(command);
                var executablePath = parts.Item1;
                var arguments = parts.Item2;

                if (!File.Exists(executablePath))
                {
                    throw new FileNotFoundException($"Executable not found: {executablePath}");
                }

                return new ProcessStartInfo
                {
                    FileName = executablePath,
                    Arguments = arguments,
                    WorkingDirectory = Path.GetDirectoryName(executablePath),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
            }
        }

        private (string, string) ParseCommand(string command)
        {
            // Handle quoted paths
            if (command.StartsWith("\""))
            {
                var endQuoteIndex = command.IndexOf("\"", 1);
                if (endQuoteIndex > 0)
                {
                    var executablePath = command.Substring(1, endQuoteIndex - 1);
                    var arguments = command.Length > endQuoteIndex + 1 ? command.Substring(endQuoteIndex + 2) : "";
                    return (executablePath, arguments);
                }
            }

            // Handle unquoted paths
            var parts = command.Split(' ', 2);
            var executable = parts[0];
            var args = parts.Length > 1 ? parts[1] : "";
            return (executable, args);
        }

        private void MonitorProcess(Service service, Process process)
        {
            Task.Run(async () =>
            {
                try
                {
                    await Task.Run(() => process.WaitForExit());
                    service.Status = ServiceStatus.Stopped;
                    service.Process = null;
                    Debug.WriteLine($"Service {service.Name} has exited");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error monitoring process for service {service.Name}: {ex.Message}");
                    service.Status = ServiceStatus.Stopped;
                    service.Process = null;
                }
            });
        }

        private async Task<string> GetServiceCommandAsync(InstalledService installedService)
        {
            var servicePath = installedService.Path;
            var serviceName = installedService.Name.ToLowerInvariant();
            var category = installedService.ServiceType.ToLowerInvariant();

            try
            {
                return category switch
                {
                    "apache" => await GetApacheCommandAsync(servicePath),
                    "mysql" => await GetMySQLCommandAsync(servicePath),
                    "redis" => await GetRedisCommandAsync(servicePath),
                    "nginx" => await GetNginxCommandAsync(servicePath),
                    "php" => await GetPHPCommandAsync(servicePath),
                    "postgresql" => await GetPostgreSQLCommandAsync(servicePath),
                    "node" => await GetNodeCommandAsync(servicePath),
                    _ => await GetGenericCommandAsync(servicePath, serviceName)
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting command for {installedService.Name}: {ex.Message}");
                return string.Empty;
            }
        }

        private async Task<string> GetApacheCommandAsync(string servicePath)
        {
            var settings = await _appSettingsService.LoadSettingsAsync();
            var selectedVersion = settings.Versions.FirstOrDefault(c => c.Service == "Apache")?.Version;

            if (!string.IsNullOrEmpty(selectedVersion))
            {
                var binPath = Path.Combine(settings.InstallDirectory, "bin", "Apache", selectedVersion, "bin");
                var httpdPath = Path.Combine(binPath, "httpd.exe");

                if (await _fileSystemService.FileExistsAsync(httpdPath))
                {
                    try
                    {
                        // Create a temporary batch file to run Apache
                        var tempBatchFile = Path.Combine(Path.GetTempPath(), $"apache_start_{Guid.NewGuid():N}.bat");
                        var batchContent = $"@echo off\ncd /d \"{binPath}\"\n\"{httpdPath}\" -D FOREGROUND\n";
                        await _fileSystemService.WriteAllTextAsync(tempBatchFile, batchContent);
                        return tempBatchFile;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error creating Apache batch file: {ex.Message}");
                        // Fallback to direct command
                        return $"cmd.exe /c \"\"{httpdPath}\" -D FOREGROUND\"";
                    }
                }
            }

            return string.Empty;
        }

        // Placeholder implementations for other service types
        private async Task<string> GetMySQLCommandAsync(string servicePath)
        {
            // TODO: Implement MySQL command logic
            await Task.Delay(1);
            return string.Empty;
        }

        private async Task<string> GetRedisCommandAsync(string servicePath)
        {
            // TODO: Implement Redis command logic
            await Task.Delay(1);
            return string.Empty;
        }

        private async Task<string> GetNginxCommandAsync(string servicePath)
        {
            // TODO: Implement Nginx command logic
            await Task.Delay(1);
            return string.Empty;
        }

        private async Task<string> GetPHPCommandAsync(string servicePath)
        {
            // TODO: Implement PHP command logic
            await Task.Delay(1);
            return string.Empty;
        }

        private async Task<string> GetPostgreSQLCommandAsync(string servicePath)
        {
            // TODO: Implement PostgreSQL command logic
            await Task.Delay(1);
            return string.Empty;
        }

        private async Task<string> GetNodeCommandAsync(string servicePath)
        {
            // TODO: Implement Node command logic
            await Task.Delay(1);
            return string.Empty;
        }

        private async Task<string> GetGenericCommandAsync(string servicePath, string serviceName)
        {
            // TODO: Implement generic command logic
            await Task.Delay(1);
            return string.Empty;
        }
    }
}
