using DevNest.Core.Models;
using System.Collections.ObjectModel;

namespace DevNest.Core.Interfaces
{

    public interface ISiteRepository
    {

        Task<IEnumerable<SiteModel>> GetSitesAsync();
        Task<IEnumerable<SiteDefinition>> GetAvailableSitesAsync();
        Task<SiteModel> CreateSiteAsync(string siteDefinitionName, string name, IProgress<string>? progress = null);
        Task DeleteSiteAsync(string siteName);
        Task<bool> SiteExistsAsync(string siteName);

    }
}
