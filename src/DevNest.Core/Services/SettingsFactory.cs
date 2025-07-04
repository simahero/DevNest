using DevNest.Core.Enums;
using DevNest.Core.Interfaces;
using DevNest.Core.State;

namespace DevNest.Core.Services
{
    public class SettingsFactory
    {
        private readonly Dictionary<ServiceType, IServiceSettingsProvider> _providers;

        public SettingsFactory(AppState appState)
        {
            _providers = new Dictionary<ServiceType, IServiceSettingsProvider>
            {
                { ServiceType.Apache, new ApacheSettingsService(appState) },
                { ServiceType.MySQL, new MySQLSettingsService() },
                { ServiceType.PHP, new PHPModelService() },
                { ServiceType.Node, new NodeModelService() },
                { ServiceType.Redis, new RedisModelService()  },
                { ServiceType.PostgreSQL, new PostgreSQLModelService() },
                { ServiceType.Nginx, new NginxSettingsService(appState) },
                { ServiceType.MongoDB, new MongoDBSettingsService() }
            };
        }

        public IEnumerable<IServiceSettingsProvider> GetAllServiceSettingsProviders()
        {
            return _providers.Values;
        }
    }
}
