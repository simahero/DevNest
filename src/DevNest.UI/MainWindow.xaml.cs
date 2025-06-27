using DevNest.Services;
using DevNest.UI.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;

namespace DevNest.UI;

public sealed partial class MainWindow : Window
{
    private readonly INavigationService _navigationService;
    private readonly StartupManager _startupManager;

    public MainWindow()
    {
        this.InitializeComponent();

        _startupManager = ServiceLocator.GetService<StartupManager>();
        _navigationService = ServiceLocator.GetService<INavigationService>();
        _navigationService.SetFrame(ContentFrame);

        // Configure custom titlebar
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // Set the default page
        MainNavigationView.SelectedItem = MainNavigationView.MenuItems[0];
        _navigationService.NavigateTo<Views.DashboardPage>(NavigationTransitions.Suppress);
    }

    private async void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        await _startupManager.CopyStarterDirOnStartup();
        await _startupManager.EnsureAliasConfs();
    }

    private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem selectedItem)
        {
            var tag = selectedItem.Tag?.ToString();
            switch (tag)
            {
                case "Dashboard":
                    _navigationService.NavigateTo<Views.DashboardPage>(NavigationTransitions.Suppress);
                    break;
                case "Services":
                    _navigationService.NavigateTo<Views.ServicesPage>(NavigationTransitions.Suppress);
                    break;
                case "Sites":
                    _navigationService.NavigateTo<Views.SitesPage>(NavigationTransitions.Suppress);
                    break;
                case "Environments":
                    _navigationService.NavigateTo<Views.EnvironmentsPage>(NavigationTransitions.Suppress);
                    break;
                case "Dumps":
                    _navigationService.NavigateTo<Views.DumpsPage>(NavigationTransitions.Suppress);
                    break;
                case "Settings":
                    _navigationService.NavigateTo<Views.SettingsPage>(NavigationTransitions.Suppress);
                    break;
            }
        }
    }
}
