using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DevNest.UI.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        [ObservableProperty]
        private object? _currentPage;

        [ObservableProperty]
        private string _selectedMenuItem = "Dashboard";

        public IRelayCommand<string> NavigateToPageCommand { get; }


        public DashboardViewModel DashboardViewModel { get; }
        public ServicesViewModel ServicesViewModel { get; }
        public SitesViewModel SitesViewModel { get; }
        public EnvironmentsViewModel EnvironmentsViewModel { get; }
        public DumpsViewModel DumpsViewModel { get; }
        public SettingsViewModel SettingsViewModel { get; }


        public MainViewModel(
            DashboardViewModel dashboardViewModel,
            ServicesViewModel servicesViewModel,
            SitesViewModel sitesViewModel,
            EnvironmentsViewModel environmentsViewModel,
            DumpsViewModel dumpsViewModel,
            SettingsViewModel settingsViewModel)
        {
            DashboardViewModel = dashboardViewModel;
            ServicesViewModel = servicesViewModel;
            SitesViewModel = sitesViewModel;
            EnvironmentsViewModel = environmentsViewModel;
            DumpsViewModel = dumpsViewModel;
            SettingsViewModel = settingsViewModel;

            Title = "DevNest";

            NavigateToPageCommand = new RelayCommand<string>(NavigateToPage);

            CurrentPage = DashboardViewModel;
        }

        private void NavigateToPage(string? pageName)
        {
            if (string.IsNullOrEmpty(pageName))
                return;

            CurrentPage = pageName switch
            {
                "Dashboard" => DashboardViewModel,
                "Services" => ServicesViewModel,
                "Sites" => SitesViewModel,
                "Environments" => EnvironmentsViewModel,
                "Dumps" => DumpsViewModel,
                "Settings" => SettingsViewModel,
                _ => DashboardViewModel
            };

            SelectedMenuItem = pageName;
        }
    }
}
