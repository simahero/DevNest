using DevNest.Core.Interfaces;
using DevNest.Core.Models;

namespace DevNest.Core.Managers.ServiceRunners
{
    internal class WSLServiceRunner : IServiceRunner
    {
        public async Task<bool> StartServiceAsync(ServiceModel service)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> StopServiceAsync(ServiceModel service)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> ToggleServiceAsync(ServiceModel service)
        {
            throw new NotImplementedException();
        }
    }
}
