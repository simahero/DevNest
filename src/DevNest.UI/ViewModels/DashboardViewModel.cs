using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;

namespace DevNest.UI.ViewModels
{
    public partial class DashboardViewModel : BaseViewModel
    {
        private readonly IServiceManager _serviceManager;
        private readonly ISiteService _siteService;
        private readonly IDumpService _dumpService;

        [ObservableProperty]
        private int _runningServicesCount;

        [ObservableProperty]
        private int _totalServicesCount;

        [ObservableProperty]
        private int _installedSitesCount;

        [ObservableProperty]
        private int _dumpFilesCount;

        public ObservableCollection<Service> RecentServices { get; } = new();
        public ObservableCollection<Site> RecentSites { get; } = new();

        public DashboardViewModel(
            IServiceManager serviceManager,
            ISiteService siteService,
            IDumpService dumpService)
        {
            _serviceManager = serviceManager;
            _siteService = siteService;
            _dumpService = dumpService;
            Title = "Dashboard";
            LoadDashboardDataCommand = new AsyncRelayCommand(LoadDashboardDataAsync);
            RefreshCommand = new AsyncRelayCommand(RefreshDashboardAsync);
        }

        public IAsyncRelayCommand LoadDashboardDataCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }

        public async Task LoadDashboardDataAsync()
        {
            IsLoading = true;
            try
            {
                // Load all data in parallel
                var servicesTask = _serviceManager.GetServicesAsync();
                var sitesTask = _siteService.GetInstalledSitesAsync();
                var dumpsTask = _dumpService.GetDumpFilesAsync();

                await Task.WhenAll(servicesTask, sitesTask, dumpsTask);

                var services = servicesTask.Result.ToList();
                var sites = sitesTask.Result.ToList();
                var dumps = dumpsTask.Result.ToList();

                // Update statistics
                TotalServicesCount = services.Count;
                RunningServicesCount = services.Count(s => s.IsRunning);
                InstalledSitesCount = sites.Count;
                DumpFilesCount = dumps.Count;

                // Update recent items (show up to 5 most recent)
                RecentServices.Clear();
                foreach (var service in services.Take(5))
                {
                    RecentServices.Add(service);
                }

                RecentSites.Clear();
                foreach (var site in sites.OrderByDescending(s => s.CreatedDate).Take(5))
                {
                    RecentSites.Add(site);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading dashboard data: {ex.Message}");
                // TODO: Show error to user
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RefreshDashboardAsync()
        {
            await LoadDashboardDataAsync();
        }

        protected override async Task OnLoadedAsync()
        {
            await LoadDashboardDataCommand.ExecuteAsync(null);
        }
    }
}
