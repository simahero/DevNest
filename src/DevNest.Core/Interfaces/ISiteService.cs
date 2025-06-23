using DevNest.Core.Models;

namespace DevNest.Core.Interfaces
{
    public interface ISiteService
    {
        Task<IEnumerable<Site>> GetInstalledSitesAsync();
        Task<IEnumerable<SiteType>> GetAvailableSiteTypesAsync();
        Task<Site> InstallSiteAsync(string siteType, string name, IProgress<string>? progress = null);
        Task RemoveSiteAsync(string siteName); Task OpenSiteAsync(string siteName);
        Task ExploreSiteAsync(string siteName);
        Task OpenSiteInVSCodeAsync(string siteName);
        Task OpenSiteInTerminalAsync(string siteName);
        Task OpenSiteInBrowserAsync(string siteName);
        Task<bool> IsSiteInstalledAsync(string siteName);
    }
}
