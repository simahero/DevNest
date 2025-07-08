using DevNest.Core.Models;

namespace DevNest.Core.Interfaces
{
    public interface IVirtualHostManager
    {
        Task CreateVirtualHostAsync(string siteName, IProgress<string>? progress = null);
        Task<int> AddVirtualHost(SiteModel site);
        Task<string> RemoveVirtualHost(SiteModel site);
    }
}
