using DevNest.Core;
using DevNest.Core.Managers.Commands;
using DevNest.Core.Dump;
using DevNest.Core.Helpers;
using DevNest.Core.Installers;
using DevNest.Core.Interfaces;
using DevNest.Core.Managers.ServiceRunners;
using DevNest.Core.Managers.Sites;
using DevNest.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using DevNest.Core.State;

namespace DevNest.UI.Services
{
    public static class CollectionExtensions
    {
        public static IServiceCollection AddCoreServices(this IServiceCollection services)
        {
            // Register both Windows and WSL implementations
            services.AddSingleton<WINServiceRunner>();
            services.AddSingleton<WSLServiceRunner>();
            services.AddSingleton<WINServiceInstaller>();
            services.AddSingleton<WslServiceInstaller>();
            services.AddSingleton<WINCommandExecutor>();
            services.AddSingleton<WSLCommandExecutor>();
            services.AddSingleton<WINCommandManager>();
            services.AddSingleton<WSLCommandManager>();

            // Register the factory that will choose the correct implementation at runtime
            services.AddSingleton<IPlatformServiceFactory, PlatformServiceFactory>();

            services.AddSingleton<AppState>();

            services.AddSingleton<ArchiveHelper>();
            services.AddSingleton<DownloadHelper>();
            services.AddSingleton<SettingsManager>();
            services.AddSingleton<ServiceManager>();
            services.AddSingleton<SiteManager>();
            services.AddSingleton<VirtualHostManager>();
            services.AddSingleton<StartupManager>();
            services.AddSingleton<VarDumperServer>();

            services.AddSingleton<SettingsFactory>();


            return services;
        }
    }
}
