using DevNest.Core.Interfaces;
using DevNest.UI.ViewModels;
using DevNest.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DevNest.UI.Services
{
    public static class CollectionExtensions
    {
        public static IServiceCollection AddUIServices(this IServiceCollection services)
        {
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IUIDispatcher, UIDispatcher>();

            // ViewModels
            services.AddSingleton<DashboardViewModel>();
            services.AddSingleton<SitesViewModel>();
            services.AddTransient<EnvironmentsViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<DumpsViewModel>();

            // Views
            services.AddTransient<DashboardPage>();
            services.AddTransient<SitesPage>();
            services.AddTransient<EnvironmentsPage>();
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
