using DevNest.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevNest.Core.Interfaces
{
    public interface IServiceInstaller
    {
        Task InstallServiceAsync(ServiceDefinition service, IProgress<string>? progress = null);
        Task UninstallServiceAsync(string serviceName, IProgress<string>? progress = null);
        Task<bool> IsServiceInstalledAsync(string serviceName);
    }
}
