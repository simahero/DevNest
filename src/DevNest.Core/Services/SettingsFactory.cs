using DevNest.Core.Enums;
using DevNest.Core.Files;
using DevNest.Core.Interfaces;

namespace DevNest.Core.Services
{
    public class SettingsFactory
    {
        private readonly Dictionary<ServiceType, IServiceSettingsProvider> _providers;

        public SettingsFactory(FileSystemManager fileSystemManager, PathManager pathManager, IServiceProvider serviceProvider)
        {
            _providers = new Dictionary<ServiceType, IServiceSettingsProvider>
            {
                { ServiceType.Apache, new ApacheSettingsService(fileSystemManager, pathManager, serviceProvider) },
                { ServiceType.MySQL, new MySQLSettingsService(fileSystemManager, pathManager) },
                { ServiceType.PHP, new PHPSettingsService(fileSystemManager, pathManager) },
                { ServiceType.Node, new NodeSettingsService() },
                { ServiceType.Redis, new RedisSettingsService()  },
                { ServiceType.PostgreSQL, new PostgreSQLSettingsService() },
                { ServiceType.Nginx, new NginxSettingsService(fileSystemManager, pathManager) },
                { ServiceType.MongoDB, new MongoDBSettingsService(fileSystemManager, pathManager) }
            };
        }

        public IEnumerable<IServiceSettingsProvider> GetAllServiceSettingsProviders()
        {
            return _providers.Values;
        }
    }
}
