using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevNest.Core;
using DevNest.Core.Models;
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
        }

        [RelayCommand]
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

        [RelayCommand]
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

        [RelayCommand]
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

        [RelayCommand]
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

        [RelayCommand]
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

        [RelayCommand]
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

        [RelayCommand]
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

        [RelayCommand]
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

        [RelayCommand]
        private async Task RefreshSitesAsync()
        {
            await LoadSitesAsync();
        }

        protected override async Task OnLoadedAsync()
        {
            await LoadSitesAsync();
            await LoadSiteDefinitionsAsync();
        }
    }
}
