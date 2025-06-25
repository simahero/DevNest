namespace DevNest.Core.Interfaces
{
    public interface IDownloadService
    {
        Task<string> DownloadToTempAsync(string url, IProgress<string>? progress = null);
    }
}
