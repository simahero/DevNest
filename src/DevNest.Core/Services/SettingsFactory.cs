using DevNest.Core.Files;
using DevNest.Core.Interfaces;

namespace DevNest.Services.Settings
{
    public class SettingsFactory
    {
        private readonly Dictionary<string, IServiceSettingsProvider> _providers;

        public SettingsFactory(FileSystemManager fileSystemManager, PathManager pathManager)
        {
            _providers = new Dictionary<string, IServiceSettingsProvider>
            {
                { "Apache", new ApacheSettingsService(fileSystemManager, pathManager) },
                { "MySQL", new MySqlSettingsService(fileSystemManager, pathManager) },
                { "PHP", new PhpSettingsService(fileSystemManager, pathManager) },
                { "Node", new NodeSettingsService() },
                { "Redis", new RedisSettingsService()  },
                { "PostgreSQL", new PostgreSQLSettingsService() },
                { "Nginx", new NginxSettingsService(fileSystemManager, pathManager) },
                { "MongoDB", new MongoDBSettingsService(fileSystemManager, pathManager) }
            };
        }

        public IEnumerable<IServiceSettingsProvider> GetAllServiceSettingsProviders()
        {
            return _providers.Values;
        }
    }
}
