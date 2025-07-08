using DevNest.Core.Interfaces;
using DevNest.Core.Models;

namespace DevNest.Core.Installers
{
    internal class WSLServiceInstaller : IServiceInstaller
    {
        public async Task InstallServiceAsync(ServiceDefinition service, IProgress<string>? progress = null)
        {
            throw new NotImplementedException();
        }

        public async Task UninstallServiceAsync(string serviceName, IProgress<string>? progress = null)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> IsServiceInstalledAsync(string serviceName)
        {
            throw new NotImplementedException();
        }
    }
}
