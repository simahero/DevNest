using DevNest.Core.Interfaces;
using DevNest.Core.Models;

namespace DevNest.Core.Installers
{
    internal class WslServiceInstaller : IServiceInstaller
    {
        public Task<InstallationResultModel> InstallServiceAsync(ServiceDefinition service, IProgress<string>? progress = null)
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsServiceInstalledAsync(string serviceName)
        {
            throw new NotImplementedException();
        }

        public Task<InstallationResultModel> UninstallServiceAsync(string serviceName, IProgress<string>? progress = null)
        {
            throw new NotImplementedException();
        }
    }
}
