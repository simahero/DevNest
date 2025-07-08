using DevNest.Core.Enums;
using DevNest.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DevNest.Core.Services
{
    public class SettingsFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public SettingsFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IEnumerable<IServiceSettingsProvider> GetAllServiceSettingsProviders()
        {
            return new List<IServiceSettingsProvider>
            {
                _serviceProvider.GetRequiredService<ApacheSettingsService>(),
                _serviceProvider.GetRequiredService<MySQLSettingsService>(),
                _serviceProvider.GetRequiredService<PHPModelService>(),
                _serviceProvider.GetRequiredService<NodeModelService>(),
                _serviceProvider.GetRequiredService<RedisModelService>(),
                _serviceProvider.GetRequiredService<PostgreSQLModelService>(),
                _serviceProvider.GetRequiredService<NginxSettingsService>(),
                _serviceProvider.GetRequiredService<MongoDBSettingsService>()
            };
        }
    }
}
