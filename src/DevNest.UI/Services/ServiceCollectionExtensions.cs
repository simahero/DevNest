using DevNest.Core.Interfaces;
using DevNest.Services;
using DevNest.UI.ViewModels;
using DevNest.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DevNest.UI.Services
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDevNestServices(this IServiceCollection services)
        {
            // Core services
            services.AddSingleton<IServiceManager, ServiceManager>();
            services.AddSingleton<IServicesReader, ServicesReader>();
            services.AddSingleton<IServiceInstallationService, ServiceInstallationService>();
            services.AddSingleton<IFileSystemService, FileSystemService>();
            services.AddSingleton<ISiteService, SiteService>();
            services.AddSingleton<ISitesReaderService, SitesReader>();
            services.AddSingleton<IAppSettingsService, AppSettingsService>();
            services.AddSingleton<IDumpService, DumpService>();

            // ViewModels
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<ServicesViewModel>();
            services.AddTransient<SitesViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<DumpsViewModel>();

            // Views
            services.AddTransient<DashboardPage>();
            services.AddTransient<ServicesPage>();
            services.AddTransient<SitesPage>();
            services.AddTransient<SettingsPage>();
            services.AddTransient<DumpsPage>();

            // Logging
            services.AddLogging(builder =>
            {
                builder.AddDebug();
                builder.AddConsole();
            });

            return services;
        }
    }
}
