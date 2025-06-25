using DevNest.Core.Interfaces;
using DevNest.Services.Commands;
using DevNest.Services.Files;
using DevNest.Services.Settings;
using DevNest.Services.Sites;
using Microsoft.Extensions.DependencyInjection;

namespace DevNest.Services
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCoreServices(this IServiceCollection services)
        {

            // Core services
            services.AddSingleton<IPathService, PathService>();
            services.AddSingleton<IServicesReader, ServicesReader>();
            services.AddSingleton<IFileSystemService, FileSystemService>();

            //Managers
            services.AddSingleton<SettingsManager, SettingsManager>();
            services.AddSingleton<InstallManager, InstallManager>();
            services.AddSingleton<ServiceManager, ServiceManager>();
            services.AddSingleton<SiteManager, SiteManager>();

            //Factories
            services.AddSingleton<SettingsFactory, SettingsFactory>();

            services.AddSingleton<IVirtualHostService, VirtualHostService>();
            services.AddSingleton<ISiteUrlDetectionService, SiteUrlDetectionService>();
            services.AddSingleton<IDownloadService, DownloadService>();
            services.AddSingleton<IArchiveExtractionService, ArchiveExtractionService>();
            services.AddSingleton<ICommandExecutionService, CommandExecutionService>();

            return services;
        }
    }
}
