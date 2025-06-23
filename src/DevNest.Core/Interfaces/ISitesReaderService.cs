using DevNest.Core.Models;

namespace DevNest.Core.Interfaces
{
    public interface ISitesReaderService
    {
        Task<IEnumerable<SiteType>> LoadSiteTypesAsync();
        Task SaveSiteTypesAsync(IEnumerable<SiteType> siteTypes);
    }
}
