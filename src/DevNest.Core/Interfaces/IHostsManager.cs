using DevNest.Core.Models;

namespace DevNest.Core.Interfaces
{
    public interface IHostsManager
    {
        Task<int> AddHostsEntry(SiteModel site);
        Task<string> RemoveHostsEntry(SiteModel site);
    }
}
