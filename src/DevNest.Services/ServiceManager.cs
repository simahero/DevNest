using DevNest.Core.Interfaces;
using DevNest.Core.Models;

namespace DevNest.Services
{
    public class ServiceManager
    {
        private readonly SettingsManager _settingsManager;

        private readonly IServicesReader _servicesReader;
        private readonly IFileSystemService _fileSystemService;
        private readonly IPathService _pathService;
        private readonly ICommandExecutionService _commandExecutionService;
        private readonly IUIDispatcher _uiDispatcher;

        private readonly List<ServiceModel> _services;

        public ServiceManager(
            IServicesReader servicesReader,
            SettingsManager settingsManager,
            IFileSystemService fileSystemService,
            ICommandExecutionService commandExecutionService,
            IPathService pathService,
            IUIDispatcher uiDispatcher)
        {
            _servicesReader = servicesReader;
            _settingsManager = settingsManager;
            _fileSystemService = fileSystemService;
            _commandExecutionService = commandExecutionService;
            _pathService = pathService;
            _uiDispatcher = uiDispatcher;
            _services = new List<ServiceModel>();
        }

        public async Task<IEnumerable<ServiceModel>> GetServicesAsync()
        {
            await RefreshServicesAsync();
            return _services.AsReadOnly();
        }

        public Task<ServiceModel?> GetServiceAsync(string name)
        {
            var service = _services.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(service);
        }

        public async Task<bool> StartServiceAsync(ServiceModel service)
        {
            if (service == null) return false;

            try
            {
                service.IsLoading = true;
                service.Status = ServiceStatus.Starting;

                var workingDirectory = GetWorkingDirectoryFromCommand(service.Command);

                var process = await _commandExecutionService.StartProcessAsync(service.Command, workingDirectory);

                if (process != null)
                {
                    process.EnableRaisingEvents = true;

                    process.Exited += (sender, e) =>
                    {
                        _uiDispatcher.TryEnqueue(() =>
                        {
                            System.Diagnostics.Debug.WriteLine($"Process exited event fired for service {service.Name}");

                            service.Status = ServiceStatus.Stopped;
                            service.Process = null;

                            System.Diagnostics.Debug.WriteLine($"Service {service.Name} status updated to Stopped due to process exit");
                        });
                    };

                    service.Process = process;
                    service.Status = ServiceStatus.Running;
                    return true;
                }
                else
                {
                    service.Status = ServiceStatus.Stopped;
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error starting service {service.Name}: {ex.Message}");
                service.Status = ServiceStatus.Stopped;
                return false;
            }
            finally
            {
                service.IsLoading = false;
            }
        }

        public async Task<bool> StopServiceAsync(ServiceModel service)
        {
            if (service == null) return false;

            try
            {
                service.IsLoading = true;
                service.Status = ServiceStatus.Stopping;

                // If we have a process attached, try to stop it gracefully
                if (service.Process != null && !service.Process.HasExited)
                {
                    try
                    {
                        if (!service.Process.CloseMainWindow())
                        {
                            service.Process.Kill();
                        }

                        if (!service.Process.WaitForExit(5000))
                        {
                            service.Process.Kill();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error stopping attached process for {service.Name}: {ex.Message}");
                        return false;
                    }
                    finally
                    {
                        service.Process?.Dispose();
                        service.Process = null;
                    }

                    service.Status = ServiceStatus.Stopped;
                    await Task.CompletedTask;
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping service {service.Name}: {ex.Message}");
                return false;
            }
            finally
            {
                service.IsLoading = false;
            }

            await Task.CompletedTask;
            return true;
        }

        public async Task<bool> ToggleServiceAsync(string serviceName)
        {
            var service = await GetServiceAsync(serviceName);
            if (service == null)
            {
                return false;
            }

            if (service.IsRunning)
            {
                return await StopServiceAsync(service);
            }
            else
            {
                return await StartServiceAsync(service);
            }
        }

        public async Task RefreshServicesAsync()
        {
            try
            {
                _services.Clear();
                var installedServices = await _servicesReader.LoadInstalledServicesAsync();
                var settings = await _settingsManager.LoadSettingsAsync();

                foreach (var service in installedServices)
                {
                    var command = await GetServiceCommandAsync(service, settings);
                    if (!string.IsNullOrEmpty(command))
                    {
                        service.Command = command;
                        service.IsSelected = IsServiceSelected(service, settings);
                        service.Status = ServiceStatus.Stopped;
                        _services.Add(service);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing services: {ex.Message}");
            }
        }

        private async Task<string> GetServiceCommandAsync(ServiceModel service, SettingsModel settings)
        {
            try
            {
                return service.ServiceType.ToLowerInvariant() switch
                {
                    "apache" => await GetApacheCommandAsync(service, settings),
                    "mysql" => await GetMySQLCommandAsync(service, settings),
                    "php" => await GetPHPCommandAsync(service, settings),
                    "nginx" => await GetNginxCommandAsync(service, settings),
                    "node" => await GetNodeCommandAsync(service, settings),
                    "redis" => await GetRedisCommandAsync(service, settings),
                    "postgresql" => await GetPostgreSQLCommandAsync(service, settings),
                    _ => await GetGenericCommandAsync(service)
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting command for {service.Name}: {ex.Message}");
                return string.Empty;
            }
        }

        private async Task<string> GetApacheCommandAsync(ServiceModel service, SettingsModel settings)
        {
            var selectedVersion = settings.Apache.Version;

            if (!string.IsNullOrEmpty(selectedVersion))
            {
                var binPath = Path.Combine(service.Path, "bin");
                var apacheRoot = service.Path;
                var httpdPath = Path.Combine(binPath, "httpd.exe");

                if (await _fileSystemService.FileExistsAsync(httpdPath))
                {
                    return $"cd /d \"{binPath}\" && \"{httpdPath}\" -d \"{apacheRoot}\" -D FOREGROUND";
                }
            }

            return string.Empty;
        }

        private async Task<string> GetMySQLCommandAsync(ServiceModel service, SettingsModel settings)
        {
            var selectedVersion = settings.MySQL.Version;

            if (!string.IsNullOrEmpty(selectedVersion))
            {
                var binPath = Path.Combine(service.Path, "bin");
                var mysqldPath = Path.Combine(binPath, "mysqld.exe");

                if (await _fileSystemService.FileExistsAsync(mysqldPath))
                {
                    return $"cd /d \"{binPath}\" && \"{mysqldPath}\"";
                }
            }

            return string.Empty;
        }

        private async Task<string> GetPHPCommandAsync(ServiceModel service, SettingsModel settings)
        {
            var selectedVersion = settings.PHP.Version;

            if (!string.IsNullOrEmpty(selectedVersion))
            {
                var phpPath = Path.Combine(service.Path, selectedVersion, "php-cgi.exe");

                if (await _fileSystemService.FileExistsAsync(phpPath))
                {
                    return $"\"{phpPath}\" -b 127.0.0.1:9000";
                }
            }

            // Fallback: look for any php-cgi.exe in the service path
            var fallbackPhpPath = Path.Combine(service.Path, "php-cgi.exe");
            if (await _fileSystemService.FileExistsAsync(fallbackPhpPath))
            {
                return $"\"{fallbackPhpPath}\" -b 127.0.0.1:9000";
            }

            return string.Empty;
        }

        private async Task<string> GetNginxCommandAsync(ServiceModel service, SettingsModel settings)
        {
            var selectedVersion = settings.Nginx.Version;

            if (!string.IsNullOrEmpty(selectedVersion))
            {
                var nginxPath = Path.Combine(service.Path, selectedVersion, "nginx.exe");

                if (await _fileSystemService.FileExistsAsync(nginxPath))
                {
                    return $"cd /d \"{Path.GetDirectoryName(nginxPath)}\" && \"{nginxPath}\"";
                }
            }

            // Fallback: look for any nginx.exe in the service path
            var fallbackNginxPath = Path.Combine(service.Path, "nginx.exe");
            if (await _fileSystemService.FileExistsAsync(fallbackNginxPath))
            {
                return $"cd /d \"{Path.GetDirectoryName(fallbackNginxPath)}\" && \"{fallbackNginxPath}\"";
            }

            return string.Empty;
        }

        private async Task<string> GetNodeCommandAsync(ServiceModel service, SettingsModel settings)
        {
            var selectedVersion = settings.Node.Version;

            if (!string.IsNullOrEmpty(selectedVersion))
            {
                var nodePath = Path.Combine(service.Path, selectedVersion, "node.exe");

                if (await _fileSystemService.FileExistsAsync(nodePath))
                {
                    return $"\"{nodePath}\"";
                }
            }

            // Fallback: look for any node.exe in the service path
            var fallbackNodePath = Path.Combine(service.Path, "node.exe");
            if (await _fileSystemService.FileExistsAsync(fallbackNodePath))
            {
                return $"\"{fallbackNodePath}\"";
            }

            return string.Empty;
        }

        private async Task<string> GetRedisCommandAsync(ServiceModel service, SettingsModel settings)
        {
            var selectedVersion = settings.Redis.Version;

            if (!string.IsNullOrEmpty(selectedVersion))
            {
                var redisPath = Path.Combine(service.Path, selectedVersion, "redis-server.exe");

                if (await _fileSystemService.FileExistsAsync(redisPath))
                {
                    return $"\"{redisPath}\"";
                }
            }

            // Fallback: look for any redis-server.exe in the service path
            var fallbackRedisPath = Path.Combine(service.Path, "redis-server.exe");
            if (await _fileSystemService.FileExistsAsync(fallbackRedisPath))
            {
                return $"\"{fallbackRedisPath}\"";
            }

            return string.Empty;
        }

        private async Task<string> GetPostgreSQLCommandAsync(ServiceModel service, SettingsModel settings)
        {
            var selectedVersion = settings.PostgreSQL.Version;

            if (!string.IsNullOrEmpty(selectedVersion))
            {
                var postgresPath = Path.Combine(service.Path, selectedVersion, "bin", "postgres.exe");

                if (await _fileSystemService.FileExistsAsync(postgresPath))
                {
                    return $"cd /d \"{Path.GetDirectoryName(postgresPath)}\" && \"{postgresPath}\"";
                }
            }

            // Fallback: look for any postgres.exe in the service path
            var fallbackPostgresPath = Path.Combine(service.Path, "bin", "postgres.exe");
            if (await _fileSystemService.FileExistsAsync(fallbackPostgresPath))
            {
                return $"cd /d \"{Path.GetDirectoryName(fallbackPostgresPath)}\" && \"{fallbackPostgresPath}\"";
            }

            return string.Empty;
        }

        private async Task<string> GetGenericCommandAsync(ServiceModel service)
        {
            // Try to find common executable names in the service directory
            var commonExecutables = new[] { "start.exe", "run.exe", "server.exe", "service.exe" };

            foreach (var executable in commonExecutables)
            {
                var executablePath = Path.Combine(service.Path, executable);
                if (await _fileSystemService.FileExistsAsync(executablePath))
                {
                    return $"\"{executablePath}\"";
                }
            }

            return string.Empty;
        }

        private static bool IsServiceSelected(ServiceModel service, SettingsModel settings)
        {
            var selectedVersion = service.ServiceType.ToLowerInvariant() switch
            {
                "apache" => settings.Apache.Version,
                "mysql" => settings.MySQL.Version,
                "php" => settings.PHP.Version,
                "nginx" => settings.Nginx.Version,
                "node" => settings.Node.Version,
                "redis" => settings.Redis.Version,
                "postgresql" => settings.PostgreSQL.Version,
                _ => string.Empty
            };

            return !string.IsNullOrEmpty(selectedVersion) &&
                   service.Name.Equals(selectedVersion, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetWorkingDirectoryFromCommand(string command)
        {
            // Extract working directory from commands that use "cd /d" pattern
            if (command.Contains("cd /d"))
            {
                var startIndex = command.IndexOf("cd /d \"") + 7;
                if (startIndex > 6)
                {
                    var endIndex = command.IndexOf("\"", startIndex);
                    if (endIndex > startIndex)
                    {
                        return command.Substring(startIndex, endIndex - startIndex);
                    }
                }
            }

            // Default to current directory
            return Environment.CurrentDirectory;
        }

        public async Task<bool> IsServiceRunningAsync(string serviceName)
        {
            var service = await GetServiceAsync(serviceName);
            if (service == null) return false;

            // Simply return the current status since process events handle updates
            return service.IsRunning;
        }
    }
}
