using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevNest.Core.Models;
using DevNest.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace DevNest.UI.ViewModels
{
    public partial class SitesViewModel : BaseViewModel
    {
        private readonly SiteManager _siteManager;

        [ObservableProperty]
        private string _selectedSiteName = string.Empty;

        [ObservableProperty]
        private SiteDefinition? _selectedSiteDefinition;

        [ObservableProperty]
        private string _installationStatus = string.Empty;

        [ObservableProperty]
        private bool _isInstalling;

        [ObservableProperty]
        private bool _showInstallationPanel;

        public ObservableCollection<SiteModel> Sites { get; } = new();
        public ObservableCollection<SiteDefinition> AvailableSiteDefinitions { get; } = new();

        public SitesViewModel(SiteManager siteManager)
        {
            _siteManager = siteManager;
            Title = "Sites";
            LoadSitesCommand = new AsyncRelayCommand(LoadSitesAsync);
            LoadSiteDefinitionsCommand = new AsyncRelayCommand(LoadSiteDefinitionsAsync);
            CreateSiteCommand = new AsyncRelayCommand(CreateSiteAsync);
            OpenSiteFolderCommand = new AsyncRelayCommand<SiteModel>(OpenSiteFolderAsync);
            OpenInVSCodeCommand = new AsyncRelayCommand<SiteModel>(OpenInVSCodeAsync);
            OpenInTerminalCommand = new AsyncRelayCommand<SiteModel>(OpenInTerminalAsync);
            OpenInBrowserCommand = new AsyncRelayCommand<SiteModel>(OpenInBrowserAsync);
            OpenSiteSettingsCommand = new AsyncRelayCommand<SiteModel>(OpenSiteSettingsAsync);
            RefreshCommand = new AsyncRelayCommand(RefreshSitesAsync);
        }

        public IAsyncRelayCommand LoadSitesCommand { get; }
        public IAsyncRelayCommand LoadSiteDefinitionsCommand { get; }
        public IAsyncRelayCommand CreateSiteCommand { get; }
        public IAsyncRelayCommand<SiteModel> OpenSiteFolderCommand { get; }
        public IAsyncRelayCommand<SiteModel> OpenInVSCodeCommand { get; }
        public IAsyncRelayCommand<SiteModel> OpenInTerminalCommand { get; }
        public IAsyncRelayCommand<SiteModel> OpenInBrowserCommand { get; }
        public IAsyncRelayCommand<SiteModel> OpenSiteSettingsCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }
        private async Task LoadSitesAsync()
        {
            IsLoading = true;
            try
            {
                var sites = await _siteManager.GetInstalledSitesAsync();
                Sites.Clear();
                foreach (var site in sites)
                {
                    Sites.Add(site);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading sites: {ex.Message}");
                // TODO: Show error to user
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadSiteDefinitionsAsync()
        {
            try
            {
                var siteDefinitions = await _siteManager.GetAvailableSiteDefinitionsAsync();
                AvailableSiteDefinitions.Clear();
                foreach (var siteDefinition in siteDefinitions)
                {
                    AvailableSiteDefinitions.Add(siteDefinition);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading site types: {ex.Message}");
                // TODO: Show error to user
            }
        }

        private async Task CreateSiteAsync()
        {
            if (SelectedSiteDefinition == null || string.IsNullOrWhiteSpace(SelectedSiteName))
                return;

            IsInstalling = true;
            ShowInstallationPanel = true;
            InstallationStatus = "Creating site...";

            try
            {
                var progress = new Progress<string>(message =>
                {
                    InstallationStatus = message;
                });

                await _siteManager.InstallSiteAsync(SelectedSiteDefinition.Name, SelectedSiteName, progress);

                InstallationStatus = "Site created successfully!";

                // Refresh sites list
                await LoadSitesAsync();

                // Reset form
                SelectedSiteDefinition = null;
                SelectedSiteName = string.Empty;

                // Hide installation panel after 3 seconds
                await Task.Delay(3000);
                ShowInstallationPanel = false;
            }
            catch (Exception ex)
            {
                InstallationStatus = $"Error creating site: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error creating site: {ex.Message}");
            }
            finally
            {
                IsInstalling = false;
            }
        }
        private async Task OpenSiteFolderAsync(SiteModel? site)
        {
            if (site == null) return;

            try
            {
                await _siteManager.ExploreSiteAsync(site.Name);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening site folder: {ex.Message}");
                // TODO: Show error to user
            }
        }
        private async Task OpenInVSCodeAsync(SiteModel? site)
        {
            if (site == null) return;

            try
            {
                await _siteManager.OpenSiteInVSCodeAsync(site.Name);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening in VS Code: {ex.Message}");
                // TODO: Show error to user
            }
        }
        private async Task OpenInTerminalAsync(SiteModel? site)
        {
            if (site == null) return;

            try
            {
                await _siteManager.OpenSiteInTerminalAsync(site.Name);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening in terminal: {ex.Message}");
                // TODO: Show error to user
            }
        }
        private async Task OpenInBrowserAsync(SiteModel? site)
        {
            if (site == null) return;

            try
            {
                await _siteManager.OpenSiteInBrowserAsync(site.Name);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening in browser: {ex.Message}");
                // TODO: Show error to user
            }
        }
        private Task OpenSiteSettingsAsync(SiteModel? site)
        {
            if (site == null) return Task.CompletedTask;

            try
            {
                // TODO: Implement site settings functionality
                System.Diagnostics.Debug.WriteLine($"Opening settings for site: {site.Name}");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening site settings: {ex.Message}");
                // TODO: Show error to user
                return Task.CompletedTask;
            }
        }
        private async Task RefreshSitesAsync()
        {
            await LoadSitesAsync();
        }

        protected override async Task OnLoadedAsync()
        {
            await LoadSitesCommand.ExecuteAsync(null);
            await LoadSiteDefinitionsCommand.ExecuteAsync(null);
        }
    }
}
