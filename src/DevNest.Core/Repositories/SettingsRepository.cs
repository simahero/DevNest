using DevNest.Core.Events;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using DevNest.Core.Enums;
using System.Linq;

namespace DevNest.Core.Repositories
{

    public class SettingsRepository : ISettingsRepository
    {
        private readonly SettingsManager _settingsManager;
        private readonly IEventBus _eventBus;

        public SettingsModel? Settings { get; private set; }

        public SettingsRepository(SettingsManager settingsManager, IEventBus eventBus)
        {
            _settingsManager = settingsManager;
            _eventBus = eventBus;
        }

        public async Task<SettingsModel> GetSettingsAsync()
        {
            Settings = await _settingsManager.LoadSettingsAsync();
            return Settings;
        }

        public async Task PopulateServiceVersionsAsync(IEnumerable<ServiceModel> installedServices, IEnumerable<ServiceDefinition> availableServices)
        {
            if (Settings == null) return;

            ClearServiceVersionCollections();

            var sortedInstalled = installedServices.OrderByDescending(s => s.Name).ToList();
            var sortedAvailable = availableServices.OrderByDescending(s => s.Name).ToList();

            foreach (var service in sortedInstalled)
            {
                var targetCollection = GetServiceSettingsCollection(service.ServiceType);
                if (targetCollection != null && !targetCollection.AvailableVersions.Contains(service.Name))
                {
                    targetCollection.AvailableVersions.Add(service.Name);
                }
            }

            foreach (var serviceDefinition in sortedAvailable)
            {
                var targetCollection = GetServiceSettingsCollection(serviceDefinition.ServiceType);
                if (targetCollection != null && !targetCollection.AvailableVersions.Any(x => x == serviceDefinition.Name))
                {
                    targetCollection.InstallableVersions.Add(serviceDefinition);
                }
            }

            foreach (var serviceType in Enum.GetValues(typeof(ServiceType)).Cast<ServiceType>())
            {
                var targetCollection = GetServiceSettingsCollection(serviceType);
                if (targetCollection != null && string.IsNullOrEmpty(targetCollection.Version) && targetCollection.AvailableVersions.Any())
                {
                    targetCollection.Version = targetCollection.AvailableVersions.First();
                }
            }

            await Task.CompletedTask;
        }

        private void ClearServiceVersionCollections()
        {
            if (Settings == null) return;

            Settings.Apache.AvailableVersions.Clear();
            Settings.Apache.InstallableVersions.Clear();
            Settings.MySQL.AvailableVersions.Clear();
            Settings.MySQL.InstallableVersions.Clear();
            Settings.PHP.AvailableVersions.Clear();
            Settings.PHP.InstallableVersions.Clear();
            Settings.Nginx.AvailableVersions.Clear();
            Settings.Nginx.InstallableVersions.Clear();
            Settings.Node.AvailableVersions.Clear();
            Settings.Node.InstallableVersions.Clear();
            Settings.Redis.AvailableVersions.Clear();
            Settings.Redis.InstallableVersions.Clear();
            Settings.PostgreSQL.AvailableVersions.Clear();
            Settings.PostgreSQL.InstallableVersions.Clear();
            Settings.MongoDB.AvailableVersions.Clear();
            Settings.MongoDB.InstallableVersions.Clear();
        }

        private ServiceSettingsModel? GetServiceSettingsCollection(ServiceType serviceType)
        {
            if (Settings == null) return null;

            return serviceType switch
            {
                ServiceType.Apache => Settings.Apache,
                ServiceType.MySQL => Settings.MySQL,
                ServiceType.PHP => Settings.PHP,
                ServiceType.Nginx => Settings.Nginx,
                ServiceType.Node => Settings.Node,
                ServiceType.Redis => Settings.Redis,
                ServiceType.PostgreSQL => Settings.PostgreSQL,
                ServiceType.MongoDB => Settings.MongoDB,
                _ => null
            };
        }
    }
}
