using DevNest.Core.Installers;
using DevNest.Core.Interfaces;
using DevNest.Core.Managers.Commands;
using DevNest.Core.Managers.ServiceRunners;
using DevNest.Core.Managers.Sites;
using Microsoft.Extensions.DependencyInjection;

namespace DevNest.Core.Services
{
    public class PlatformServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ISettingsRepository _settingsRepository;

        public PlatformServiceFactory(IServiceProvider serviceProvider, ISettingsRepository settingsRepository)
        {
            _serviceProvider = serviceProvider;
            _settingsRepository = settingsRepository;
        }

        public IServiceLoader GetServiceLoader()
        {
            var settings = _settingsRepository.Settings ?? throw new InvalidOperationException("Settings are not loaded.");
            if (settings.UseWSL)
            {
                return _serviceProvider.GetRequiredService<WSLServiceLoader>();
            }
            else
            {
                return _serviceProvider.GetRequiredService<WINServiceLoader>();
            }
        }

        public IServiceRunner GetServiceRunner()
        {
            var settings = _settingsRepository.Settings ?? throw new InvalidOperationException("Settings are not loaded.");
            if (settings.UseWSL)
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
            var settings = _settingsRepository.Settings ?? throw new InvalidOperationException("Settings are not loaded.");
            if (settings.UseWSL)
            {
                return _serviceProvider.GetRequiredService<WSLServiceInstaller>();
            }
            else
            {
                return _serviceProvider.GetRequiredService<WINServiceInstaller>();
            }
        }

        public IVirtualHostManager GetVirtualHostManager()
        {
            var settings = _settingsRepository.Settings ?? throw new InvalidOperationException("Settings are not loaded.");
            if (settings.UseWSL)
            {
                return _serviceProvider.GetRequiredService<WSLVirtualHostManager>();
            }
            else
            {
                return _serviceProvider.GetRequiredService<WINVirtualHostManager>();
            }
        }

        public ICommandExecutor GetCommandExecutor()
        {
            var settings = _settingsRepository.Settings ?? throw new InvalidOperationException("Settings are not loaded.");
            if (settings.UseWSL)
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
            var settings = _settingsRepository.Settings ?? throw new InvalidOperationException("Settings are not loaded.");
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