using DevNest.Core.Interfaces;
using DevNest.Core.Managers.Commands;
using DevNest.Core.Managers.ServiceRunners;
using DevNest.Core.Installers;
using DevNest.Core.State;
using Microsoft.Extensions.DependencyInjection;

namespace DevNest.Core.Services
{
    public class PlatformServiceFactory : IPlatformServiceFactory
    {
        private readonly AppState _appState;
        private readonly IServiceProvider _serviceProvider;

        public PlatformServiceFactory(AppState appState, IServiceProvider serviceProvider)
        {
            _appState = appState;
            _serviceProvider = serviceProvider;
        }

        public IServiceRunner GetServiceRunner()
        {
            var useWSL = _appState.Settings?.UseWLS ?? false;

            if (useWSL)
            {
                return _serviceProvider.GetRequiredService<WSLServiceRunner>();
            }
            else
            {
                return _serviceProvider.GetRequiredService<WINServiceRunner>();
            }
        }

        public IServiceInstaller GetServiceInstaller()
        {
            var useWSL = _appState.Settings?.UseWLS ?? false;

            if (useWSL)
            {
                return _serviceProvider.GetRequiredService<WslServiceInstaller>();
            }
            else
            {
                return _serviceProvider.GetRequiredService<WINServiceInstaller>();
            }
        }

        public ICommandExecutor GetCommandExecutor()
        {
            var useWSL = _appState.Settings?.UseWLS ?? false;

            if (useWSL)
            {
                return _serviceProvider.GetRequiredService<WSLCommandExecutor>();
            }
            else
            {
                return _serviceProvider.GetRequiredService<WINCommandExecutor>();
            }
        }

        public ICommandManager GetCommandManager()
        {
            var useWSL = _appState.Settings?.UseWLS ?? false;

            if (useWSL)
            {
                return _serviceProvider.GetRequiredService<WSLCommandManager>();
            }
            else
            {
                return _serviceProvider.GetRequiredService<WINCommandManager>();
            }
        }
    }
}
