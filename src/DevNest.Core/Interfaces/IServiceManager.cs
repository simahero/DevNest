using DevNest.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DevNest.Core.Interfaces
{
    public interface IServiceManager
    {
        Task<IEnumerable<Service>> GetServicesAsync();
        Task<Service?> GetServiceAsync(string name);
        Task StartServiceAsync(string serviceName);
        Task StopServiceAsync(string serviceName);
        Task<bool> IsServiceRunningAsync(string serviceName);
        Task RefreshServicesAsync();
    }
}
