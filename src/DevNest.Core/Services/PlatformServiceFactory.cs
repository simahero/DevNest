using DevNest.Core.Installers;
using DevNest.Core.Interfaces;
using DevNest.Core.Managers.Commands;
using DevNest.Core.Managers.ServiceRunners;
using DevNest.Core.Managers.Sites;
using DevNest.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace DevNest.Core.Services
{
    public class PlatformServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public PlatformServiceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IServiceLoader GetServiceLoader(SettingsModel settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (settings.UseWSL)
            {
                return _serviceProvider.GetRequiredService<WSLServiceLoader>();
            }
            else
            {
                return _serviceProvider.GetRequiredService<WINServiceLoader>();
            }
        }

        public IServiceRunner GetServiceRunner(SettingsModel settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (settings.UseWSL)
            {
                return _serviceProvider.GetRequiredService<WSLServiceRunner>();
            }
            else
            {
                return _serviceProvider.GetRequiredService<WINServiceRunner>();
            }
        }

        public IServiceInstaller GetServiceInstaller(SettingsModel settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (settings.UseWSL)
            {
                return _serviceProvider.GetRequiredService<WSLServiceInstaller>();
            }
            else
            {
                return _serviceProvider.GetRequiredService<WINServiceInstaller>();
            }
        }

        public IVirtualHostManager GetVirtualHostManager(SettingsModel settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (settings.UseWSL)
            {
                return _serviceProvider.GetRequiredService<WSLVirtualHostManager>();
            }
            else
            {
                return _serviceProvider.GetRequiredService<WINVirtualHostManager>();
            }
        }

        public ICommandExecutor GetCommandExecutor(SettingsModel settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (settings.UseWSL)
            {
                return _serviceProvider.GetRequiredService<WSLCommandExecutor>();
            }
            else
            {
                return _serviceProvider.GetRequiredService<WINCommandExecutor>();
            }
        }

        public ICommandManager GetCommandManager(SettingsModel settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (settings.UseWSL)
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