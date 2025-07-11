using System.ComponentModel;
using DevNest.Core.Models;
using DevNest.Core.Helpers;
using DevNest.Core.State;

namespace DevNest.Core.Managers
{
    public class AutoSaveManager : IDisposable
    {
        private readonly AppState _appState;
        private readonly SettingsManager _settingsManager;
        private readonly int _debounceMs;
        private CancellationTokenSource? _cts;
        private SettingsModel? _settings;

        public AutoSaveManager(AppState appState, SettingsManager settingsManager, int debounceMs = 0)
        {
            _appState = appState;
            _settingsManager = settingsManager;
            _debounceMs = debounceMs;

            // Listen to AppState property changes to detect when Settings object is recreated
            if (_appState is INotifyPropertyChanged appStateNpc)
            {
                appStateNpc.PropertyChanged += AppState_PropertyChanged;
            }

            AttachToSettings();
        }

        private void AttachToSettings()
        {
            if (_settings is INotifyPropertyChanged oldNpc)
            {
                oldNpc.PropertyChanged -= Settings_PropertyChanged;
            }
            DetachFromNestedObjects();

            _settings = _appState.Settings;
            if (_settings is INotifyPropertyChanged npc)
            {
                npc.PropertyChanged += Settings_PropertyChanged;
            }
            AttachToNestedObjects();
        }

        private void AttachToNestedObjects()
        {
            if (_settings == null) return;

            if (_settings.Apache is INotifyPropertyChanged apache)
                apache.PropertyChanged += Settings_PropertyChanged;
            if (_settings.MySQL is INotifyPropertyChanged mysql)
                mysql.PropertyChanged += Settings_PropertyChanged;
            if (_settings.PHP is INotifyPropertyChanged php)
                php.PropertyChanged += Settings_PropertyChanged;
            if (_settings.Node is INotifyPropertyChanged node)
                node.PropertyChanged += Settings_PropertyChanged;
            if (_settings.Redis is INotifyPropertyChanged redis)
                redis.PropertyChanged += Settings_PropertyChanged;
            if (_settings.PostgreSQL is INotifyPropertyChanged postgresql)
                postgresql.PropertyChanged += Settings_PropertyChanged;
            if (_settings.Nginx is INotifyPropertyChanged nginx)
                nginx.PropertyChanged += Settings_PropertyChanged;
            if (_settings.MongoDB is INotifyPropertyChanged mongodb)
                mongodb.PropertyChanged += Settings_PropertyChanged;
        }

        private void DetachFromNestedObjects()
        {
            if (_settings == null) return;

            if (_settings.Apache is INotifyPropertyChanged apache)
                apache.PropertyChanged -= Settings_PropertyChanged;
            if (_settings.MySQL is INotifyPropertyChanged mysql)
                mysql.PropertyChanged -= Settings_PropertyChanged;
            if (_settings.PHP is INotifyPropertyChanged php)
                php.PropertyChanged -= Settings_PropertyChanged;
            if (_settings.Node is INotifyPropertyChanged node)
                node.PropertyChanged -= Settings_PropertyChanged;
            if (_settings.Redis is INotifyPropertyChanged redis)
                redis.PropertyChanged -= Settings_PropertyChanged;
            if (_settings.PostgreSQL is INotifyPropertyChanged postgresql)
                postgresql.PropertyChanged -= Settings_PropertyChanged;
            if (_settings.Nginx is INotifyPropertyChanged nginx)
                nginx.PropertyChanged -= Settings_PropertyChanged;
            if (_settings.MongoDB is INotifyPropertyChanged mongodb)
                mongodb.PropertyChanged -= Settings_PropertyChanged;
        }

        public void RefreshSettingsSubscription()
        {
            AttachToSettings();
        }

        private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SettingsModel.UseWSL))
            {
                _ = HandleUseWSLChange();
                return;
            }

            DebounceSave();
        }

        private void AppState_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppState.Settings))
            {
                // Settings object was recreated, reattach event listeners
                AttachToSettings();
            }
        }

        private async Task HandleUseWSLChange()
        {
            if (_settings == null) return;

            var useWSLValue = _settings.UseWSL;

            DetachFromNestedObjects();

            await SaveBaseSettingsAsync();

            PathHelper.SetUseWSL(useWSLValue);

            await _appState.Reload();

            AttachToSettings();
        }

        private void DebounceSave()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(_debounceMs, token);
                    if (!token.IsCancellationRequested)
                    {
                        await SaveSettingsAsync();
                    }
                }
                catch (TaskCanceledException) { }
            }, token);
        }

        private async Task SaveSettingsAsync()
        {
            await SaveBaseSettingsAsync();
            await SavePlatformSettingsAsync();
        }

        private async Task SaveBaseSettingsAsync()
        {
            if (_settings == null) return;

            var baseIniData = _settingsManager.ConvertSettingsToIni(_settings).ToString();
            await FileSystemHelper.WriteFileWithRetryAsync(PathHelper.BaseSettingsPath, baseIniData);
        }

        private async Task SavePlatformSettingsAsync()
        {
            if (_settings == null) return;

            var platformIniData = _settingsManager.ConvertPlatformSettingsToIni(_settings).ToString();
            await FileSystemHelper.WriteFileWithRetryAsync(PathHelper.SettingsPath, platformIniData);
        }

        public void Dispose()
        {
            if (_settings is INotifyPropertyChanged npc)
            {
                npc.PropertyChanged -= Settings_PropertyChanged;
            }
            if (_appState is INotifyPropertyChanged appStateNpc)
            {
                appStateNpc.PropertyChanged -= AppState_PropertyChanged;
            }
            DetachFromNestedObjects();
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
