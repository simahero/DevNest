using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using DevNest.Core.Exceptions;

namespace DevNest.UI.ViewModels
{
    public partial class SitesViewModel : BaseViewModel
    {
        private readonly ISiteService _siteService;

        [ObservableProperty]
        private string _selectedSiteName = string.Empty;

        [ObservableProperty]
        private SiteType? _selectedSiteType;

        [ObservableProperty]
        private string _installationStatus = string.Empty;

        [ObservableProperty]
        private bool _isInstalling;

        [ObservableProperty]
        private bool _showInstallationPanel;

        public ObservableCollection<Site> Sites { get; } = new();
        public ObservableCollection<SiteType> AvailableSiteTypes { get; } = new();

        public SitesViewModel(ISiteService siteService)
        {
            _siteService = siteService;
            Title = "Sites";
            LoadSitesCommand = new AsyncRelayCommand(LoadSitesAsync);
            LoadSiteTypesCommand = new AsyncRelayCommand(LoadSiteTypesAsync);
            CreateSiteCommand = new AsyncRelayCommand(CreateSiteAsync);
            OpenSiteFolderCommand = new AsyncRelayCommand<Site>(OpenSiteFolderAsync);
            OpenInVSCodeCommand = new AsyncRelayCommand<Site>(OpenInVSCodeAsync);
            OpenInTerminalCommand = new AsyncRelayCommand<Site>(OpenInTerminalAsync);
            OpenInBrowserCommand = new AsyncRelayCommand<Site>(OpenInBrowserAsync);
            OpenSiteSettingsCommand = new AsyncRelayCommand<Site>(OpenSiteSettingsAsync);
            RefreshCommand = new AsyncRelayCommand(RefreshSitesAsync);
        }

        public IAsyncRelayCommand LoadSitesCommand { get; }
        public IAsyncRelayCommand LoadSiteTypesCommand { get; }
        public IAsyncRelayCommand CreateSiteCommand { get; }
        public IAsyncRelayCommand<Site> OpenSiteFolderCommand { get; }
        public IAsyncRelayCommand<Site> OpenInVSCodeCommand { get; }
        public IAsyncRelayCommand<Site> OpenInTerminalCommand { get; }
        public IAsyncRelayCommand<Site> OpenInBrowserCommand { get; }
        public IAsyncRelayCommand<Site> OpenSiteSettingsCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }
        private async Task LoadSitesAsync()
        {
            IsLoading = true;
            try
            {
                var sites = await _siteService.GetInstalledSitesAsync();
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

        private async Task LoadSiteTypesAsync()
        {
            try
            {
                var siteTypes = await _siteService.GetAvailableSiteTypesAsync();
                AvailableSiteTypes.Clear();
                foreach (var siteType in siteTypes)
                {
                    AvailableSiteTypes.Add(siteType);
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
            if (SelectedSiteType == null || string.IsNullOrWhiteSpace(SelectedSiteName))
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

                await _siteService.InstallSiteAsync(SelectedSiteType.Name, SelectedSiteName, progress);

                InstallationStatus = "Site created successfully!";

                // Refresh sites list
                await LoadSitesAsync();

                // Reset form
                SelectedSiteType = null;
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

        private async Task OpenSiteFolderAsync(Site? site)
        {
            if (site == null) return;

            try
            {
                await _siteService.ExploreSiteAsync(site.Name);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening site folder: {ex.Message}");
                // TODO: Show error to user
            }
        }

        private async Task OpenInVSCodeAsync(Site? site)
        {
            if (site == null) return;

            try
            {
                await _siteService.OpenSiteInVSCodeAsync(site.Name);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening in VS Code: {ex.Message}");
                // TODO: Show error to user
            }
        }

        private async Task OpenInTerminalAsync(Site? site)
        {
            if (site == null) return;

            try
            {
                await _siteService.OpenSiteInTerminalAsync(site.Name);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening in terminal: {ex.Message}");
                // TODO: Show error to user
            }
        }

        private async Task OpenInBrowserAsync(Site? site)
        {
            if (site == null) return;

            try
            {
                await _siteService.OpenSiteInBrowserAsync(site.Name);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening in browser: {ex.Message}");
                // TODO: Show error to user
            }
        }

        private async Task OpenSiteSettingsAsync(Site? site)
        {
            if (site == null) return;

            try
            {
                // TODO: Implement site settings functionality
                System.Diagnostics.Debug.WriteLine($"Opening settings for site: {site.Name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening site settings: {ex.Message}");
                // TODO: Show error to user
            }
        }
        private async Task RefreshSitesAsync()
        {
            await LoadSitesAsync();
        }

        protected override async Task OnLoadedAsync()
        {
            await LoadSitesCommand.ExecuteAsync(null);
            await LoadSiteTypesCommand.ExecuteAsync(null);
        }
    }
}
