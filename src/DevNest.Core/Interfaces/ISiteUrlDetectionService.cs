namespace DevNest.Core.Interfaces
{
    public interface ISiteUrlDetectionService
    {
        Task<string> DetectSiteUrlAsync(Core.Models.SiteModel site);
    }
}
