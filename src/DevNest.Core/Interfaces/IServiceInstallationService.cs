using DevNest.Core.Models;
using System;
using System.Threading.Tasks;

namespace DevNest.Core.Interfaces
{
    public class InstallationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? InstalledPath { get; set; }
        public Exception? Exception { get; set; }
    }

    public interface IServiceInstallationService
    {
        Task<InstallationResult> InstallServiceAsync(ServiceDefinition serviceDefinition, IProgress<string>? progress = null);
        Task<bool> IsServiceInstalledAsync(string serviceName);
        Task<InstallationResult> UninstallServiceAsync(string serviceName, IProgress<string>? progress = null);
    }
}
