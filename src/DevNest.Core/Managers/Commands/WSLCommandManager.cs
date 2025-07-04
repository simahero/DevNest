using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using DevNest.Core.Enums;
using System.Diagnostics;

namespace DevNest.Core.Managers.Commands
{
    public class WSLCommandManager : ICommandManager
    {
        public async Task<(string, string)> GetCommand(ServiceModel service, SettingsModel settings)
        {
            try
            {
                return service.ServiceType switch
                {
                    ServiceType.Apache => await GetApacheCommandAsync(service, settings),
                    ServiceType.MySQL => await GetMySQLCommandAsync(service, settings),
                    ServiceType.Nginx => await GetNginxCommandAsync(service, settings),
                    ServiceType.Node => await GetNodeCommandAsync(service, settings),
                    ServiceType.Redis => await GetRedisCommandAsync(service, settings),
                    ServiceType.PostgreSQL => await GetPostgreSQLCommandAsync(service, settings),
                    ServiceType.MongoDB => await GetMongoDBCommandAsync(service, settings),
                    ServiceType.PHP => await GetPHPCommandAsync(service, settings),
                    _ => await Task.FromResult((string.Empty, string.Empty)),
                };
            }
            catch (Exception)
            {
                return (string.Empty, string.Empty);
            }
        }

        private Task<(string, string)> GetApacheCommandAsync(ServiceModel service, SettingsModel settings)
        {
            var selectedVersion = settings.Apache.Version;
            if (!string.IsNullOrEmpty(selectedVersion))
            {
                var binPath = Path.Combine(service.Path, "bin");
                var apacheRoot = service.Path;

                return Task.FromResult(($"httpd.exe -d \"{apacheRoot}\" -D FOREGROUND", binPath));
            }
            return Task.FromResult((string.Empty, string.Empty));
        }

        private Task<(string, string)> GetMySQLCommandAsync(ServiceModel service, SettingsModel settings)
        {
            var selectedVersion = settings.MySQL.Version;
            if (!string.IsNullOrEmpty(selectedVersion))
            {
                var binPath = Path.Combine(service.Path, "bin");

                return Task.FromResult(($"mysqld.exe", binPath));
            }
            return Task.FromResult((string.Empty, string.Empty));
        }

        private Task<(string, string)> GetNginxCommandAsync(ServiceModel service, SettingsModel settings)
        {
            var selectedVersion = settings.Nginx.Version;
            if (!string.IsNullOrEmpty(selectedVersion))
            {
                return Task.FromResult(($"nginx.exe", service.Path));
            }
            return Task.FromResult((string.Empty, string.Empty));
        }

        private Task<(string, string)> GetNodeCommandAsync(ServiceModel service, SettingsModel settings)
        {
            var selectedVersion = settings.Node.Version;
            if (!string.IsNullOrEmpty(selectedVersion))
            {
                return Task.FromResult(($"node.exe", service.Path));
            }
            return Task.FromResult((string.Empty, string.Empty));
        }

        private Task<(string, string)> GetRedisCommandAsync(ServiceModel service, SettingsModel settings)
        {
            var selectedVersion = settings.Redis.Version;
            if (!string.IsNullOrEmpty(selectedVersion))
            {
                return Task.FromResult(($"redis-server.exe", service.Path));
            }
            return Task.FromResult((string.Empty, string.Empty));
        }

        private Task<(string, string)> GetPostgreSQLCommandAsync(ServiceModel service, SettingsModel settings)
        {
            var selectedVersion = settings.PostgreSQL.Version;
            if (!string.IsNullOrEmpty(selectedVersion))
            {
                var binPath = Path.Combine(service.Path, "bin");
                return Task.FromResult(($"postgres.exe", binPath));
            }
            return Task.FromResult((string.Empty, string.Empty));
        }

        private Task<(string, string)> GetMongoDBCommandAsync(ServiceModel service, SettingsModel settings)
        {
            var selectedVersion = settings.MongoDB.Version;
            if (!string.IsNullOrEmpty(selectedVersion))
            {
                var binPath = Path.Combine(service.Path, "bin");
                var configPath = Path.Combine(service.Path, "bin", "mongod.cfg");

                return Task.FromResult(($"mongod.exe --config \"{configPath}\"", binPath));
            }
            return Task.FromResult((string.Empty, string.Empty));
        }

        private Task<(string, string)> GetPHPCommandAsync(ServiceModel service, SettingsModel settings)
        {
            var selectedVersion = settings.PHP.Version;
            if (!string.IsNullOrEmpty(selectedVersion))
            {
                return Task.FromResult(($"php-cgi.exe -b 127.0.0.1:9003", service.Path));
            }
            return Task.FromResult((string.Empty, string.Empty));
        }
    }
}
