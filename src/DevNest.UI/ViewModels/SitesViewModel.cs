using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevNest.Core;
using DevNest.Core.Files;
using DevNest.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DevNest.UI.ViewModels
{
    public partial class SitesViewModel : BaseViewModel
    {
        private readonly SiteManager _siteManager;
        private readonly SettingsManager _settingsManager;

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

        public SitesViewModel(SiteManager siteManager, SettingsManager settingsManager)
        {
            _siteManager = siteManager;
            _settingsManager = settingsManager;
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

                await LoadSitesAsync();

                SelectedSiteDefinition = null;
                SelectedSiteName = string.Empty;

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
        private void OpenSiteFolder(SiteModel? site)
        {
            if (site == null) return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = site.Path,
                    UseShellExecute = true
                });
            }
            catch (Exception) { }
        }

        [RelayCommand]
        private void OpenInVSCode(SiteModel? site)
        {
            if (site == null) return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "code",
                    Arguments = site.Path,
                    UseShellExecute = true
                });
            }
            catch (Exception) { }
        }

        [RelayCommand]
        private void OpenInTerminal(SiteModel? site)
        {
            if (site == null)
                return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "pwsh.exe",
                    Arguments = $"-NoExit -Command \"cd '{site.Path}'\"",
                    UseShellExecute = true
                });
            }
            catch (Exception) { }
        }

        [RelayCommand]
        private void ShareWithTunnel(SiteModel? site)
        {
            if (site == null) return;

            try
            {
                if (site.ShareProcess != null && !site.ShareProcess.HasExited)
                {
                    try
                    {
                        // Kill the entire tree
                        var killTree = new ProcessStartInfo
                        {
                            FileName = "taskkill",
                            Arguments = $"/PID {site.ShareProcess.Id} /T /F",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        Process.Start(killTree)?.WaitForExit();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to kill process tree: {ex.Message}");
                    }
                    finally
                    {
                        if (site.ShareProcess != null)
                        {
                            site.ShareProcess.Dispose();
                            site.ShareProcess = null;
                        }
                    }

                    return;
                }

                var processInfo = new ProcessStartInfo
                {
                    FileName = "ngrok",
                    Arguments = $"http --hostname=osprey-epic-seagull.ngrok-free.app --host-header={site.Name.ToLower()}.test:80 80",
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                };

                var process = Process.Start(processInfo);
                if (process != null)
                {
                    site.ShareProcess = process;

                    process.EnableRaisingEvents = true;
                    process.Exited += (sender, e) =>
                    {
                        site.ShareProcess = null;
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error toggling tunnel: {ex.Message}");
                // Ensure we clean up if something went wrong
                if (site.ShareProcess != null)
                {
                    try
                    {
                        site.ShareProcess.Kill();
                        site.ShareProcess.Dispose();
                    }
                    catch { }
                    site.ShareProcess = null;
                }
            }
        }

        [RelayCommand]
        private void OpenInBrowser(SiteModel? site)
        {
            if (site == null) return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = site.Url,
                    UseShellExecute = true
                });
            }
            catch (Exception) { }
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
