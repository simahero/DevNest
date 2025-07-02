using DevNest.Core.Enums;
using DevNest.Core.Files;
using DevNest.Core.Interfaces;

namespace DevNest.Core.Services
{
    public class SettingsFactory
    {
        private readonly Dictionary<ServiceType, IServiceSettingsProvider> _providers;

        public SettingsFactory(IServiceProvider serviceProvider)
        {
            _providers = new Dictionary<ServiceType, IServiceSettingsProvider>
            {
                { ServiceType.Apache, new ApacheSettingsService(serviceProvider) },
                { ServiceType.MySQL, new MySQLSettingsService() },
                { ServiceType.PHP, new PHPSettingsService() },
                { ServiceType.Node, new NodeSettingsService() },
                { ServiceType.Redis, new RedisSettingsService()  },
                { ServiceType.PostgreSQL, new PostgreSQLSettingsService() },
                { ServiceType.Nginx, new NginxSettingsService(serviceProvider) },
                { ServiceType.MongoDB, new MongoDBSettingsService() }
            };
        }

        public IEnumerable<IServiceSettingsProvider> GetAllServiceSettingsProviders()
        {
            return _providers.Values;
        }
    }
}
