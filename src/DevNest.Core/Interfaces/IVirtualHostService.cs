namespace DevNest.Core.Interfaces
{
    public interface IVirtualHostService
    {
        Task CreateVirtualHostAsync(string siteName, IProgress<string>? progress = null);
    }
}
