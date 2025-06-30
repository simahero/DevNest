using DevNest.Core.Commands;
using DevNest.Core.Files;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;

namespace DevNest.Core
{
    public class ServiceManager
    {
        private readonly SettingsManager _settingsManager;
        private readonly LogManager _logManager = null!;
        private readonly CommandManager _commandManager;

        private readonly ServicesReader _servicesReader;
        private readonly FileSystemManager _fileSystemManager;
        private readonly PathManager _pathManager;
        private readonly IUIDispatcher _uiDispatcher;

        private readonly List<ServiceModel> _services;

        public ServiceManager(
            SettingsManager settingsManager,
            CommandManager commandManager,
            ServicesReader servicesReader,
            FileSystemManager fileSystemManager,
            PathManager pathManager,
            IUIDispatcher uiDispatcher)
        {
            _settingsManager = settingsManager;
            _servicesReader = servicesReader;
            _fileSystemManager = fileSystemManager;
            _commandManager = commandManager;
            _pathManager = pathManager;
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

                var workingDirectory = service.WorkingDirectory ?? Environment.CurrentDirectory;
                var process = await _commandManager.StartProcessAsync(service.Command, workingDirectory, default);

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
                    var (command, workingDirectory) = await GetServiceCommandAsync(service, settings);
                    if (!string.IsNullOrEmpty(command) && !string.IsNullOrEmpty(workingDirectory))
                    {
                        service.Command = command;
                        service.WorkingDirectory = workingDirectory;
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

        private async Task<(string, string)> GetServiceCommandAsync(ServiceModel service, SettingsModel settings)
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
                    "mongodb" => await GetMongoDBCommandAsync(service, settings),
                    _ => await GetGenericCommandAsync(service)
                };
            }
            catch (Exception)
            {
                return (string.Empty, string.Empty);
            }
        }

        private async Task<(string, string)> GetApacheCommandAsync(ServiceModel service, SettingsModel settings)
        {
            var selectedVersion = settings.Apache.Version;
            if (!string.IsNullOrEmpty(selectedVersion))
            {
                var binPath = Path.Combine(service.Path, "bin");
                var apacheRoot = service.Path;
                var httpdPath = Path.Combine(binPath, "httpd.exe");
                if (await _fileSystemManager.FileExistsAsync(httpdPath))
                {
                    return ($"\"{httpdPath}\" -d \"{apacheRoot}\" -D FOREGROUND", binPath);
                }
            }
            return (string.Empty, string.Empty);
        }

        private async Task<(string, string)> GetMySQLCommandAsync(ServiceModel service, SettingsModel settings)
        {
            var selectedVersion = settings.MySQL.Version;
            if (!string.IsNullOrEmpty(selectedVersion))
            {
                var binPath = Path.Combine(service.Path, "bin");
                var mysqldPath = Path.Combine(binPath, "mysqld.exe");
                if (await _fileSystemManager.FileExistsAsync(mysqldPath))
                {
                    return ($"\"{mysqldPath}\"", binPath);
                }
            }
            return (string.Empty, string.Empty);
        }

        private async Task<(string, string)> GetPHPCommandAsync(ServiceModel service, SettingsModel settings)
        {
            var selectedVersion = settings.PHP.Version;
            if (!string.IsNullOrEmpty(selectedVersion))
            {
                var phpPath = Path.Combine(service.Path, selectedVersion, "php-cgi.exe");
                if (await _fileSystemManager.FileExistsAsync(phpPath))
                {
                    return ($"\"{phpPath}\" -b 127.0.0.1:9000", Path.GetDirectoryName(phpPath)!);
                }
            }
            return (string.Empty, string.Empty);
        }

        private async Task<(string, string)> GetNginxCommandAsync(ServiceModel service, SettingsModel settings)
        {
            var selectedVersion = settings.Nginx.Version;
            if (!string.IsNullOrEmpty(selectedVersion))
            {
                var nginxPath = Path.Combine(service.Path, "nginx.exe");
                if (await _fileSystemManager.FileExistsAsync(nginxPath))
                {
                    return ($"\"{nginxPath}\"", Path.GetDirectoryName(nginxPath)!);
                }
            }
            return (string.Empty, string.Empty);
        }

        private async Task<(string, string)> GetNodeCommandAsync(ServiceModel service, SettingsModel settings)
        {
            var selectedVersion = settings.Node.Version;
            if (!string.IsNullOrEmpty(selectedVersion))
            {
                var nodePath = Path.Combine(service.Path, "node.exe");
                if (await _fileSystemManager.FileExistsAsync(nodePath))
                {
                    return ($"\"{nodePath}\"", Path.GetDirectoryName(nodePath)!);
                }
            }

            return (string.Empty, string.Empty);
        }

        private async Task<(string, string)> GetRedisCommandAsync(ServiceModel service, SettingsModel settings)
        {
            var selectedVersion = settings.Redis.Version;
            if (!string.IsNullOrEmpty(selectedVersion))
            {
                var redisPath = Path.Combine(service.Path, "redis-server.exe");
                if (await _fileSystemManager.FileExistsAsync(redisPath))
                {
                    return ($"\"redis-server.exe\" --service-start", Path.GetDirectoryName(redisPath)!);
                }
            }
            return (string.Empty, string.Empty);
        }

        private async Task<(string, string)> GetPostgreSQLCommandAsync(ServiceModel service, SettingsModel settings)
        {
            var selectedVersion = settings.PostgreSQL.Version;
            if (!string.IsNullOrEmpty(selectedVersion))
            {
                var postgresPath = Path.Combine(service.Path, "bin", "postgres.exe");
                if (await _fileSystemManager.FileExistsAsync(postgresPath))
                {
                    return ($"\"{postgresPath}\"", Path.GetDirectoryName(postgresPath)!);
                }
            }
            return (string.Empty, string.Empty);
        }

        private async Task<(string, string)> GetMongoDBCommandAsync(ServiceModel service, SettingsModel settings)
        {
            var selectedVersion = settings.MongoDB.Version;
            if (!string.IsNullOrEmpty(selectedVersion))
            {
                var mongoDBPath = Path.Combine(service.Path, "bin", "mongod.exe");
                if (await _fileSystemManager.FileExistsAsync(mongoDBPath))
                {
                    return ($"\"{mongoDBPath}\"", Path.GetDirectoryName(mongoDBPath)!);
                }
            }
            return (string.Empty, string.Empty);
        }

        private Task<(string, string)> GetGenericCommandAsync(ServiceModel service)
        {
            return Task.FromResult((string.Empty, string.Empty));
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
                "mongodb" => settings.MongoDB.Version,
                _ => string.Empty
            };

            return !string.IsNullOrEmpty(selectedVersion) && service.Name.Equals(selectedVersion, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<bool> IsServiceRunningAsync(string serviceName)
        {
            var service = await GetServiceAsync(serviceName);
            if (service == null) return false;

            return service.IsRunning;
        }
    }
}
