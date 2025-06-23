using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;

namespace DevNest.UI.ViewModels
{
    public partial class SettingsViewModel : BaseViewModel
    {
        private readonly IAppSettingsService _appSettingsService;
        private AppSettings? _currentSettings;

        [ObservableProperty]
        private bool _startWithWindows;

        [ObservableProperty]
        private bool _minimizeToSystemTray;

        [ObservableProperty]
        private bool _autoVirtualHosts;

        [ObservableProperty]
        private bool _autoCreateDatabase;

        [ObservableProperty]
        private string _installDirectory = @"C:\DevNest"; [ObservableProperty]
        private ObservableCollection<ServiceVersion> _serviceVersions = new();

        [ObservableProperty]
        private string _selectedInstallDirectory = @"C:\DevNest";

        private bool _isInitializing = true;

        public SettingsViewModel(IAppSettingsService appSettingsService)
        {
            _appSettingsService = appSettingsService;
            Title = "Settings";
            SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
            ResetSettingsCommand = new AsyncRelayCommand(ResetSettingsAsync);
            BrowseFolderCommand = new AsyncRelayCommand(BrowseFolderAsync);
        }

        public IAsyncRelayCommand SaveSettingsCommand { get; }
        public IAsyncRelayCommand ResetSettingsCommand { get; }
        public IAsyncRelayCommand BrowseFolderCommand { get; }        // Override the method from ObservableObject to handle property changes
        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            // Don't auto-save during initialization or for non-settings properties
            if (_isInitializing || string.IsNullOrEmpty(e.PropertyName))
                return;

            // Only auto-save for actual settings properties
            if (e.PropertyName == nameof(StartWithWindows) ||
                e.PropertyName == nameof(MinimizeToSystemTray) ||
                e.PropertyName == nameof(AutoVirtualHosts) ||
                e.PropertyName == nameof(AutoCreateDatabase) ||
                e.PropertyName == nameof(InstallDirectory))
            {
                // Fire and forget - don't await to avoid blocking the UI
                _ = Task.Run(SaveSettingsAsync);
            }
        }

        private async Task LoadSettingsAsync()
        {
            IsLoading = true;
            try
            {
                _currentSettings = await _appSettingsService.LoadSettingsAsync();

                // Update properties from loaded settings
                StartWithWindows = _currentSettings.StartWithWindows;
                MinimizeToSystemTray = _currentSettings.MinimizeToSystemTray;
                AutoVirtualHosts = _currentSettings.AutoVirtualHosts;
                AutoCreateDatabase = _currentSettings.AutoCreateDatabase;
                InstallDirectory = _currentSettings.InstallDirectory;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
                // TODO: Show error to user
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SaveSettingsAsync()
        {
            if (_currentSettings == null)
                return;

            IsLoading = true;
            try
            {
                // Update settings object with current property values
                _currentSettings.StartWithWindows = StartWithWindows;
                _currentSettings.MinimizeToSystemTray = MinimizeToSystemTray;
                _currentSettings.AutoVirtualHosts = AutoVirtualHosts;
                _currentSettings.AutoCreateDatabase = AutoCreateDatabase;
                _currentSettings.InstallDirectory = InstallDirectory;

                await _appSettingsService.SaveSettingsAsync(_currentSettings);

                // TODO: Show success message to user
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
                // TODO: Show error to user
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ResetSettingsAsync()
        {
            IsLoading = true;
            try
            {
                await _appSettingsService.ResetSettingsAsync();
                await LoadSettingsAsync(); // Reload default settings

                // TODO: Show success message to user
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error resetting settings: {ex.Message}");
                // TODO: Show error to user
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadVersionsAsync()
        {
            if (_currentSettings == null) return;

            try
            {
                var installDirectory = _currentSettings.InstallDirectory;
                ServiceVersions.Clear();

                foreach (var serviceVersion in _currentSettings.Versions)
                {
                    try
                    {
                        serviceVersion.AvailableVersions.Clear();
                        var serviceCategoryPath = System.IO.Path.Combine(installDirectory, "bin", serviceVersion.Service);

                        if (System.IO.Directory.Exists(serviceCategoryPath))
                        {
                            var versionDirectories = System.IO.Directory.GetDirectories(serviceCategoryPath)
                                .Select(dir => System.IO.Path.GetFileName(dir))
                                .OrderBy(version => version)
                                .ToList();

                            foreach (var version in versionDirectories)
                            {
                                serviceVersion.AvailableVersions.Add(version);
                            }
                        }

                        if (string.IsNullOrEmpty(serviceVersion.Version) && serviceVersion.AvailableVersions.Count > 0)
                        {
                            serviceVersion.Version = serviceVersion.AvailableVersions.First();
                        }

                        ServiceVersions.Add(serviceVersion);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading versions for {serviceVersion.Service}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading service versions: {ex.Message}");
            }
        }

        private async Task BrowseFolderAsync()
        {
            try
            {
                // TODO: Implement folder picker using Windows.Storage.Pickers.FolderPicker
                // For now, just update the property
                System.Diagnostics.Debug.WriteLine("Browse folder functionality needs to be implemented");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error browsing folder: {ex.Message}");
            }
        }
        protected override async Task OnLoadedAsync()
        {
            _isInitializing = true;
            await LoadSettingsAsync();
            await LoadVersionsAsync();
            _isInitializing = false;
        }
    }
}
