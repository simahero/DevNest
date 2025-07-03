using DevNest.Core;
using DevNest.Core.Commands;
using DevNest.Core.Dump;
using DevNest.Core.Helpers;
using DevNest.Core.Services;
using DevNest.Core.Sites;
using Microsoft.Extensions.DependencyInjection;

namespace DevNest.UI.Services
{
    public static class CollectionExtensions
    {
        public static IServiceCollection AddCoreServices(this IServiceCollection services)
        {

            services.AddSingleton<ArchiveHelper>();
            services.AddSingleton<DownloadHelper>();
            services.AddSingleton<SettingsManager>();
            services.AddSingleton<CommandManager>();
            services.AddSingleton<ServiceManager>();
            services.AddSingleton<InstallManager>();
            services.AddSingleton<SiteManager>();
            services.AddSingleton<VirtualHostManager>();
            services.AddSingleton<StartupManager>();
            services.AddSingleton<VarDumperServer>();

            services.AddSingleton<SettingsFactory>();


            return services;
        }
    }
}
