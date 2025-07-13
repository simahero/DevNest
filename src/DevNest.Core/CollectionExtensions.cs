using DevNest.Core;
using DevNest.Core.Helpers;
using DevNest.Core.Installers;
using DevNest.Core.Interfaces;
using DevNest.Core.Managers.Commands;
using DevNest.Core.Managers.Dump;
using DevNest.Core.Managers.ServiceRunners;
using DevNest.Core.Managers.Sites;
using DevNest.Core.Managers.SMTP;
using DevNest.Core.Services;
using DevNest.Core.State;
using DevNest.Core.Repositories;
using Microsoft.Extensions.DependencyInjection;
using DevNest.Core.Events;

namespace DevNest.UI.Services
{
    public static class CollectionExtensions
    {
        public static IServiceCollection AddCoreServices(this IServiceCollection services)
        {

            services.AddSingleton<WINServiceRunner>();
            services.AddSingleton<WSLServiceRunner>();
            services.AddSingleton<WINServiceInstaller>();
            services.AddSingleton<WSLServiceInstaller>();
            services.AddSingleton<WINCommandExecutor>();
            services.AddSingleton<WSLCommandExecutor>();
            services.AddSingleton<WINCommandManager>();
            services.AddSingleton<WSLCommandManager>();
            services.AddSingleton<WINServiceLoader>();
            services.AddSingleton<WSLServiceLoader>();
            services.AddSingleton<WINVirtualHostManager>();
            services.AddSingleton<WSLVirtualHostManager>();

            services.AddSingleton<PlatformServiceFactory>();

            services.AddSingleton<ISettingsRepository, SettingsRepository>();
            services.AddSingleton<ISiteRepository, SiteRepository>();
            services.AddSingleton<IServiceRepository, ServiceRepository>();

            services.AddSingleton<AppState>();

            services.AddSingleton<ArchiveHelper>();
            services.AddSingleton<DownloadHelper>();
            services.AddSingleton<VarDumperServer>();
            services.AddSingleton<SettingsFactory>();
            services.AddSingleton<SMTP>();

            services.AddSingleton<StartupManager>();

            services.AddSingleton<ApacheSettingsService>();
            services.AddSingleton<MySQLSettingsService>();
            services.AddSingleton<PHPModelService>();
            services.AddSingleton<NodeModelService>();
            services.AddSingleton<RedisModelService>();
            services.AddSingleton<PostgreSQLModelService>();
            services.AddSingleton<NginxSettingsService>();
            services.AddSingleton<MongoDBSettingsService>();

            return services;
        }
    }
}
