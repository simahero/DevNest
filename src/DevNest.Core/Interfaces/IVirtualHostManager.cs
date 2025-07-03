using DevNest.Core.Models;

namespace DevNest.Core.Interfaces
{
    public interface IVirtualHostManager
    {
        Task<int> AddVirtualHost(SiteModel site);
        Task<string> RemoveVirtualHost(SiteModel site);
    }
}
