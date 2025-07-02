using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevNest.Core;
using DevNest.Core.Models;
using System;
using System.Threading.Tasks;

namespace DevNest.UI.ViewModels
{
    public partial class SettingsViewModel : BaseViewModel
    {
        private readonly SettingsManager _settingsManager;
        private SettingsModel? _currentSettings;

        [ObservableProperty]
        private bool _startWithWindows;

        [ObservableProperty]
        private bool _minimizeToSystemTray;

        [ObservableProperty]
        private bool _autoVirtualHosts;

        [ObservableProperty]
        private bool _autoCreateDatabase;

        [ObservableProperty]
        private string _installDirectory = @"C:\DevNest";

        [ObservableProperty]
        private string _selectedInstallDirectory = @"C:\DevNest";

        private bool _isInitializing = true;

        public SettingsViewModel(SettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
            Title = "Settings";
        }


        partial void OnStartWithWindowsChanged(bool value)
        {
            if (!_isInitializing && _currentSettings != null)
                _currentSettings.StartWithWindows = value;
        }

        partial void OnMinimizeToSystemTrayChanged(bool value)
        {
            if (!_isInitializing && _currentSettings != null)
                _currentSettings.MinimizeToSystemTray = value;
        }

        partial void OnAutoVirtualHostsChanged(bool value)
        {
            if (!_isInitializing && _currentSettings != null)
                _currentSettings.AutoVirtualHosts = value;
        }

        partial void OnAutoCreateDatabaseChanged(bool value)
        {
            if (!_isInitializing && _currentSettings != null)
                _currentSettings.AutoCreateDatabase = value;
        }

        private async Task LoadSettingsAsync()
        {
            _isInitializing = true;
            IsLoading = true;
            try
            {
                _currentSettings = await _settingsManager.LoadSettingsAsync();

                // Update ViewModel properties from loaded settings
                StartWithWindows = _currentSettings.StartWithWindows;
                MinimizeToSystemTray = _currentSettings.MinimizeToSystemTray;
                AutoVirtualHosts = _currentSettings.AutoVirtualHosts;
                AutoCreateDatabase = _currentSettings.AutoCreateDatabase;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
                // TODO: Show error to user
            }
            finally
            {
                IsLoading = false;
                _isInitializing = false;
            }
        }

        [RelayCommand]
        private void BrowseFolder()
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
            _isInitializing = false;
        }
    }
}
