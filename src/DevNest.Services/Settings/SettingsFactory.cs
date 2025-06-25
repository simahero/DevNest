using DevNest.Core.Interfaces;

namespace DevNest.Services.Settings
{
    public class SettingsFactory
    {
        private readonly Dictionary<string, IServiceSettingsProvider> _providers;

        public SettingsFactory(IFileSystemService fileSystemService, IPathService pathService)
        {
            _providers = new Dictionary<string, IServiceSettingsProvider>
            {
                { "Apache", new ApacheSettingsService(fileSystemService, pathService) },
                { "MySQL", new MySqlSettingsService() },
                { "PHP", new PhpSettingsService() },
                { "Node", new NodeSettingsService() },
                { "Redis", new RedisSettingsService()  },
                { "PostgreSQL", new PostgreSQLSettingsService() },
                { "Nginx", new NginxSettingsService() }
            };
        }

        public IEnumerable<IServiceSettingsProvider> GetAllServiceSettingsProviders()
        {
            return _providers.Values;
        }
    }
}
