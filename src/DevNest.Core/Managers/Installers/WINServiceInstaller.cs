using DevNest.Core.Helpers;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;

namespace DevNest.Core.Installers
{
    internal class WINServiceInstaller : IServiceInstaller
    {
        public async Task InstallServiceAsync(ServiceDefinition service, IProgress<string>? progress = null)
        {
            string downloadUrl = service.Url;
            string archivePath = await DownloadHelper.DownloadToTempAsync(downloadUrl, progress);

            string extractPath = Path.Combine(PathHelper.BinPath, service.ServiceType.ToString(), service.Name);
            await ArchiveHelper.ExtractAsync(archivePath, extractPath, service.HasAdditionalDir, progress);
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
