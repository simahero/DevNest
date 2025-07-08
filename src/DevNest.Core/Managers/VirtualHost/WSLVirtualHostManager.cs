using DevNest.Core.Helpers;
using DevNest.Core.Models;
using DevNest.Core.Interfaces;
using System.Diagnostics;

namespace DevNest.Core.Managers.Sites
{
    public class WSLVirtualHostManager : IVirtualHostManager
    {
        private readonly ISettingsRepository _settingsRepository;

        public WSLVirtualHostManager(ISettingsRepository settingsRepository)
        {
            _settingsRepository = settingsRepository;
        }

        public Task<int> AddVirtualHost(SiteModel site)
        {
            throw new NotImplementedException();
        }

        public Task CreateVirtualHostAsync(string siteName, IProgress<string>? progress = null)
        {
            throw new NotImplementedException();
        }

        public Task<string> RemoveVirtualHost(SiteModel site)
        {
            throw new NotImplementedException();
        }
    }
}
