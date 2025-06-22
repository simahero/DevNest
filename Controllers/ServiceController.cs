using DevNest.Readers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DevNest.Controllers
{
    public enum ServiceStatus
    {
        Stopped,
        Running,
        Starting,
        Stopping
    }

    public class ServiceInfo : INotifyPropertyChanged
    {
        private ServiceStatus _status;
        private bool _isLoading;
        private Process? _process;

        public required string Name { get; set; }
        public required string DisplayName { get; set; }
        public required string Command { get; set; }

        public ServiceStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(IsRunning));
                    OnPropertyChanged(nameof(StatusColor));
                    OnPropertyChanged(nameof(ActionButtonText));
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }

        public Process? Process
        {
            get => _process;
            set
            {
                _process = value;
                OnPropertyChanged(nameof(Process));
            }
        }

        public bool IsRunning => Status == ServiceStatus.Running;
        public string StatusColor => IsRunning ? "#10B981" : "#EF4444";
        public string ActionButtonText => IsRunning ? "Stop" : "Start";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class ServiceController : INotifyPropertyChanged
    {
        private static readonly string DevNestBinPath = @"C:\DevNest\bin";
        private readonly ServicesReader _ServicesReader;
        private readonly AppSettingsController _appSettings;

        public ObservableCollection<ServiceInfo> Services { get; private set; }
        public ServiceController()
        {
            Services = new ObservableCollection<ServiceInfo>();
            _ServicesReader = new ServicesReader(DevNestBinPath);
            _appSettings = AppSettingsController.Instance;
        }

        public async Task LoadServicesAsync()
        {
            if (!Directory.Exists(DevNestBinPath))
            {
                Directory.CreateDirectory(DevNestBinPath);
            }

            Services.Clear();

            await Task.Run(() =>
            {
                var servicesToAdd = new List<ServiceInfo>();

                var installedServices = _ServicesReader.LoadInstalledServices();

                foreach (var installedService in installedServices)
                {
                    var command = GetServiceCommand(installedService);

                    if (!string.IsNullOrEmpty(command))
                    {
                        var serviceInfo = new ServiceInfo
                        {
                            Name = installedService.Name,
                            DisplayName = $"{installedService.Category} - {installedService.Name}",
                            Command = command,
                            Status = ServiceStatus.Stopped
                        };

                        servicesToAdd.Add(serviceInfo);
                    }
                }

                return servicesToAdd;
            }).ContinueWith(task =>
            {
                foreach (var service in task.Result)
                {
                    Services.Add(service);
                }

            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public async Task<bool> StartServiceAsync(ServiceInfo serviceInfo)
        {
            if (serviceInfo.IsLoading || serviceInfo.IsRunning) return false;

            serviceInfo.IsLoading = true;
            serviceInfo.Status = ServiceStatus.Starting;

            try
            {
                return await Task.Run(() =>
                {
                    var command = serviceInfo.Command;

                    if (string.IsNullOrEmpty(command))
                    {
                        Debug.WriteLine($"No command found for service: {serviceInfo.Name}");
                        return false;
                    }

                    ProcessStartInfo processStartInfo; try
                    {
                        // Check if command is a batch file
                        if (command.EndsWith(".bat") && File.Exists(command))
                        {
                            processStartInfo = new ProcessStartInfo
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
                            // Handle commands that use cmd.exe wrapper or change directory first
                            string cmdArgs;
                            if (command.StartsWith("cmd.exe /c"))
                            {
                                cmdArgs = command.Substring("cmd.exe /c".Length).Trim();
                            }
                            else
                            {
                                cmdArgs = $"/c {command}";
                            }

                            processStartInfo = new ProcessStartInfo
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
                                Debug.WriteLine($"Executable not found: {executablePath}");
                                return false;
                            }

                            processStartInfo = new ProcessStartInfo
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

                        Debug.WriteLine($"Starting service {serviceInfo.Name} with command: {command}");

                        var process = Process.Start(processStartInfo);
                        if (process != null)
                        {
                            serviceInfo.Process = process;
                            serviceInfo.Status = ServiceStatus.Running;

                            // Monitor the process
                            MonitorProcess(serviceInfo, process);

                            Debug.WriteLine($"Service {serviceInfo.Name} started successfully with PID: {process.Id}");
                            return true;
                        }

                        Debug.WriteLine($"Failed to start process for service {serviceInfo.Name}");
                        return false;
                    }
                    catch (System.ComponentModel.Win32Exception win32Ex)
                    {
                        Debug.WriteLine($"Win32 error starting service {serviceInfo.Name}: {win32Ex.Message} (Error Code: {win32Ex.ErrorCode})");
                        Debug.WriteLine($"Command was: {command}");
                        return false;
                    }
                    catch (System.Runtime.InteropServices.COMException comEx)
                    {
                        Debug.WriteLine($"COM error starting service {serviceInfo.Name}: {comEx.Message} (HRESULT: 0x{comEx.HResult:X8})");
                        Debug.WriteLine($"Command was: {command}");
                        return false;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"General error starting service {serviceInfo.Name}: {ex.Message}");
                        Debug.WriteLine($"Exception type: {ex.GetType().Name}");
                        Debug.WriteLine($"Command was: {command}");
                        return false;
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting service {serviceInfo.Name}: {ex.Message}");
                serviceInfo.Status = ServiceStatus.Stopped;
                return false;
            }
            finally
            {
                serviceInfo.IsLoading = false;
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

        // Method to add a new service manually (for custom services not detected automatically)
        public void AddCustomService(string serviceName, string command)
        {
            var serviceInfo = new ServiceInfo
            {
                Name = serviceName,
                DisplayName = serviceName,
                Command = command,
                Status = ServiceStatus.Stopped
            };

            Services.Add(serviceInfo);
        }

        // Method to remove a service
        public void RemoveService(string serviceName)
        {
            var serviceToRemove = Services.FirstOrDefault(s => s.Name == serviceName);
            if (serviceToRemove != null)
            {
                // Stop the service if it's running
                if (serviceToRemove.IsRunning)
                {
                    _ = StopServiceAsync(serviceToRemove);
                }
                Services.Remove(serviceToRemove);
            }
        }

        // Method to update a service command
        public void UpdateServiceCommand(string serviceName, string newCommand)
        {
            var serviceInfo = Services.FirstOrDefault(s => s.Name == serviceName);
            if (serviceInfo != null)
            {
                serviceInfo.Command = newCommand;
            }
        }

        private string GetServiceCommand(InstalledService installedService)
        {
            var servicePath = installedService.Path;
            var serviceName = installedService.Name.ToLowerInvariant();
            var category = installedService.Category.ToLowerInvariant();

            try
            {
                switch (category)
                {
                    case "apache":
                        return GetApacheCommand(servicePath);

                    case "mysql":
                        return GetMySQLCommand(servicePath);

                    case "redis":
                        return GetRedisCommand(servicePath);

                    case "nginx":
                        return GetNginxCommand(servicePath);

                    case "php":
                        return GetPHPCommand(servicePath);

                    case "postgresql":
                        return GetPostgreSQLCommand(servicePath);

                    case "node":
                        return GetNodeCommand(servicePath);

                    default:
                        // Try to find common executables
                        return GetGenericCommand(servicePath, serviceName);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting command for {installedService.Name}: {ex.Message}");
                return string.Empty;
            }
        }

        private string GetApacheCommand(string servicePath)
        {
            var installDirectory = AppSettingsController.Instance.InstallDirectory;
            var selectedVersion = _appSettings.Versions.FirstOrDefault(c => c.Name == "Apache")?.SelectedVersion;

            if (!string.IsNullOrEmpty(selectedVersion))
            {
                var binPath = Path.Combine(installDirectory, "bin", "Apache", selectedVersion, "bin");
                var httpdPath = Path.Combine(binPath, "httpd.exe");

                if (File.Exists(httpdPath))
                {
                    try
                    {
                        // Create a temporary batch file to run Apache
                        var tempBatchFile = Path.Combine(Path.GetTempPath(), $"apache_start_{Guid.NewGuid():N}.bat");
                        var batchContent = $"@echo off\ncd /d \"{binPath}\"\n\"{httpdPath}\" -D FOREGROUND\n";
                        File.WriteAllText(tempBatchFile, batchContent);

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

        private string GetMySQLCommand(string servicePath)
        {
            // Look for mysqld.exe
            var possiblePaths = new[]
            {
                Path.Combine(servicePath, "bin", "mysqld.exe"),
                Path.Combine(servicePath, "mysqld.exe")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return $"\"{path}\" --console";
                }
            }

            return string.Empty;
        }

        private string GetRedisCommand(string servicePath)
        {
            // Look for redis-server.exe
            var possiblePaths = new[]
            {
                Path.Combine(servicePath, "redis-server.exe"),
                Path.Combine(servicePath, "bin", "redis-server.exe")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return $"\"{path}\"";
                }
            }

            return string.Empty;
        }

        private string GetNginxCommand(string servicePath)
        {
            // Look for nginx.exe
            var possiblePaths = new[]
            {
                Path.Combine(servicePath, "nginx.exe"),
                Path.Combine(servicePath, "bin", "nginx.exe")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    var binDir = Path.GetDirectoryName(path);
                    return $"cd /d \"{binDir}\" && nginx.exe";
                }
            }

            return string.Empty;
        }

        private string GetPHPCommand(string servicePath)
        {
            // Look for php.exe
            var possiblePaths = new[]
            {
                Path.Combine(servicePath, "php.exe"),
                Path.Combine(servicePath, "bin", "php.exe")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return $"\"{path}\" -S localhost:8000";
                }
            }

            return string.Empty;
        }

        private string GetPostgreSQLCommand(string servicePath)
        {
            // Look for postgres.exe
            var possiblePaths = new[]
            {
                Path.Combine(servicePath, "bin", "postgres.exe"),
                Path.Combine(servicePath, "postgres.exe")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return $"\"{path}\" -D \"{Path.Combine(servicePath, "data")}\"";
                }
            }

            return string.Empty;
        }

        private string GetNodeCommand(string servicePath)
        {
            // Look for node.exe
            var possiblePaths = new[]
            {
                Path.Combine(servicePath, "node.exe"),
                Path.Combine(servicePath, "bin", "node.exe")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return $"\"{path}\" --version"; // Simple command to test Node.js
                }
            }

            return string.Empty;
        }

        private string GetGenericCommand(string servicePath, string serviceName)
        {
            // Try to find common executable patterns
            var extensions = new[] { ".exe", ".bat", ".cmd" };
            var commonNames = new[] { serviceName, "server", "daemon", "service" };

            foreach (var name in commonNames)
            {
                foreach (var ext in extensions)
                {
                    var fileName = name + ext;
                    var possiblePaths = new[]
                    {
                        Path.Combine(servicePath, fileName),
                        Path.Combine(servicePath, "bin", fileName)
                    };

                    foreach (var path in possiblePaths)
                    {
                        if (File.Exists(path))
                        {
                            return $"\"{path}\"";
                        }
                    }
                }
            }

            return string.Empty;
        }

        private void MonitorProcess(ServiceInfo serviceInfo, Process process)
        {
            Task.Run(() =>
            {
                try
                {
                    process.WaitForExit();
                    serviceInfo.Status = ServiceStatus.Stopped;
                    serviceInfo.Process = null;
                }
                catch (System.Runtime.InteropServices.COMException comEx)
                {
                    Debug.WriteLine($"COM error monitoring process for {serviceInfo.Name}: {comEx.Message}");
                    serviceInfo.Status = ServiceStatus.Stopped;
                    serviceInfo.Process = null;
                }
                catch (System.ComponentModel.Win32Exception win32Ex)
                {
                    Debug.WriteLine($"Win32 error monitoring process for {serviceInfo.Name}: {win32Ex.Message}");
                    serviceInfo.Status = ServiceStatus.Stopped;
                    serviceInfo.Process = null;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error monitoring process for {serviceInfo.Name}: {ex.Message}");
                    serviceInfo.Status = ServiceStatus.Stopped;
                    serviceInfo.Process = null;
                }
            });
        }
        public async Task<bool> StopServiceAsync(ServiceInfo serviceInfo)
        {
            if (serviceInfo.IsLoading || !serviceInfo.IsRunning) return false;

            serviceInfo.IsLoading = true;
            serviceInfo.Status = ServiceStatus.Stopping;

            try
            {
                return await Task.Run(() =>
                {
                    if (serviceInfo.Process != null && !serviceInfo.Process.HasExited)
                    {
                        try
                        {
                            serviceInfo.Process.Kill();
                            serviceInfo.Process.WaitForExit(5000); // Wait up to 5 seconds
                            serviceInfo.Process = null;
                            serviceInfo.Status = ServiceStatus.Stopped;

                            // Clean up temporary batch file if it exists
                            CleanupTempBatchFile(serviceInfo.Command);

                            return true;
                        }
                        catch (System.Runtime.InteropServices.COMException comEx)
                        {
                            Debug.WriteLine($"COM error killing process for {serviceInfo.Name}: {comEx.Message}");
                        }
                        catch (System.ComponentModel.Win32Exception win32Ex)
                        {
                            Debug.WriteLine($"Win32 error killing process for {serviceInfo.Name}: {win32Ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error killing process for {serviceInfo.Name}: {ex.Message}");
                        }
                    }

                    serviceInfo.Status = ServiceStatus.Stopped;
                    serviceInfo.Process = null;

                    // Clean up temporary batch file if it exists
                    CleanupTempBatchFile(serviceInfo.Command);

                    return true;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error stopping service {serviceInfo.Name}: {ex.Message}");
                return false;
            }
            finally
            {
                serviceInfo.IsLoading = false;
            }
        }

        private void CleanupTempBatchFile(string command)
        {
            try
            {
                if (command.EndsWith(".bat") && File.Exists(command) && command.Contains(Path.GetTempPath()))
                {
                    File.Delete(command);
                    Debug.WriteLine($"Cleaned up temporary batch file: {command}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cleaning up temporary batch file {command}: {ex.Message}");
            }
        }

        public async Task StopAllServicesAsync()
        {
            var runningServices = Services.Where(s => s.IsRunning && !s.IsLoading).ToList();

            var stopTasks = runningServices.Select(service => StopServiceAsync(service));
            await Task.WhenAll(stopTasks);
        }
        public async Task RefreshServiceStatusAsync()
        {
            await Task.Run(() =>
            {
                foreach (var serviceInfo in Services)
                {
                    try
                    {
                        if (serviceInfo.Process != null)
                        {
                            if (serviceInfo.Process.HasExited)
                            {
                                serviceInfo.Status = ServiceStatus.Stopped;
                                serviceInfo.Process = null;
                            }
                            else
                            {
                                serviceInfo.Status = ServiceStatus.Running;
                            }
                        }
                        else
                        {
                            serviceInfo.Status = ServiceStatus.Stopped;
                        }
                    }
                    catch (System.Runtime.InteropServices.COMException comEx)
                    {
                        Debug.WriteLine($"COM error refreshing status for {serviceInfo.Name}: {comEx.Message}");
                        serviceInfo.Status = ServiceStatus.Stopped;
                        serviceInfo.Process = null;
                    }
                    catch (System.ComponentModel.Win32Exception win32Ex)
                    {
                        Debug.WriteLine($"Win32 error refreshing status for {serviceInfo.Name}: {win32Ex.Message}");
                        serviceInfo.Status = ServiceStatus.Stopped;
                        serviceInfo.Process = null;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error refreshing status for {serviceInfo.Name}: {ex.Message}");
                        serviceInfo.Status = ServiceStatus.Stopped;
                        serviceInfo.Process = null;
                    }
                }
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
