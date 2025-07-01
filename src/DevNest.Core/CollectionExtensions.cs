using DevNest.Core;
using DevNest.Core.Commands;
using DevNest.Core.Dump;
using DevNest.Core.Files;
using DevNest.Core.Services;
using DevNest.Core.Sites;
using Microsoft.Extensions.DependencyInjection;

namespace DevNest.UI.Services
{
    public static class CollectionExtensions
    {
        public static IServiceCollection AddCoreServices(this IServiceCollection services)
        {

            services.AddSingleton<PathManager>();
            services.AddSingleton<FileSystemManager>();
            services.AddSingleton<LogManager>();
            services.AddSingleton<ArchiveExtractionManager>();
            services.AddSingleton<DownloadManager>();
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
