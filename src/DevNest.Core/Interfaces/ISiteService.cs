using DevNest.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DevNest.Core.Interfaces
{
    public interface ISiteService
    {
        Task<IEnumerable<Site>> GetInstalledSitesAsync();
        Task<Site> InstallSiteAsync(string siteType, string name);
        Task RemoveSiteAsync(string siteName);
        Task OpenSiteAsync(string siteName);
        Task ExploreSiteAsync(string siteName);
        Task<bool> IsSiteInstalledAsync(string siteName);
    }
}
