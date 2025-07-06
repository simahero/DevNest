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

        public DashboardViewModel DashboardViewModel { get; }
        public SitesViewModel SitesViewModel { get; }
        public EnvironmentsViewModel EnvironmentsViewModel { get; }
        public DumpsViewModel DumpsViewModel { get; }
        public SettingsViewModel SettingsViewModel { get; }
        public MailViewModel MailViewModel { get; }


        public MainViewModel(
            DashboardViewModel dashboardViewModel,
            SitesViewModel sitesViewModel,
            EnvironmentsViewModel environmentsViewModel,
            DumpsViewModel dumpsViewModel,
            SettingsViewModel settingsViewModel,
            MailViewModel mailViewModel)
        {
            DashboardViewModel = dashboardViewModel;
            SitesViewModel = sitesViewModel;
            EnvironmentsViewModel = environmentsViewModel;
            DumpsViewModel = dumpsViewModel;
            SettingsViewModel = settingsViewModel;
            MailViewModel = mailViewModel;

            Title = "DevNest";

            CurrentPage = DashboardViewModel;
        }

        [RelayCommand]
        private void NavigateToPage(string? pageName)
        {
            if (string.IsNullOrEmpty(pageName))
                return;

            CurrentPage = pageName switch
            {
                "Dashboard" => DashboardViewModel,
                "Sites" => SitesViewModel,
                "Environments" => EnvironmentsViewModel,
                "Dumps" => DumpsViewModel,
                "Settings" => SettingsViewModel,
                "Mail" => MailViewModel,
                _ => DashboardViewModel
            };

            SelectedMenuItem = pageName;
        }
    }
}
