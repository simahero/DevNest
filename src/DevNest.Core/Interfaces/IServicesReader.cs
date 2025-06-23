using DevNest.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DevNest.Core.Interfaces
{
    public interface IServicesReader
    {
        Task<List<InstalledService>> LoadInstalledServicesAsync();
        Task<List<ServiceDefinition>> LoadAvailableServicesAsync();
    }
}
