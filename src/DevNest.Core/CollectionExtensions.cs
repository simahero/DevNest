using DevNest.Core;
using DevNest.Core.Commands;
using DevNest.Core.Files;
using DevNest.Core.Interfaces;
using DevNest.Core.Sites;
using DevNest.Services.Settings;
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
            services.AddSingleton<ServicesReader>();
            services.AddSingleton<ServiceManager>();
            services.AddSingleton<SiteManager>();
            services.AddSingleton<InstallManager>();
            services.AddSingleton<VirtualHostManager>();
            services.AddSingleton<StartupManager>();

            services.AddSingleton<SettingsFactory>();

            // Register IServicesReader as ServicesReader
            services.AddSingleton<IServicesReader>(sp => sp.GetRequiredService<ServicesReader>());

            return services;
        }
    }
}
