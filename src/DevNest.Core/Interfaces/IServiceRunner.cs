using DevNest.Core.Models;

namespace DevNest.Core.Interfaces
{
    public interface IServiceRunner
    {
        Task<bool> StartServiceAsync(ServiceModel service);
        Task<bool> StopServiceAsync(ServiceModel service);
        Task<bool> ToggleServiceAsync(ServiceModel service);
    }
}
