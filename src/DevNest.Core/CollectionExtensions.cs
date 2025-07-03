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

namespace DevNest.UI.Services
{
    public static class CollectionExtensions
    {
        public static IServiceCollection AddCoreServices(this IServiceCollection services)
        {

            var useWSL = false;

            if (useWSL)
            {
                services.AddSingleton<IServiceRunner, WSLServiceRunner>();
                services.AddSingleton<IServiceInstaller, WslServiceInstaller>();
                services.AddSingleton<ICommandExecutor, WSLCommandExecutor>();
            }
            else
            {
                services.AddSingleton<IServiceRunner, WINServiceRunner>();
                services.AddSingleton<IServiceInstaller, WINServiceInstaller>();
                services.AddSingleton<ICommandExecutor, WINCommandExecutor>();
            }

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
