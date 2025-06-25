using DevNest.UI.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevNest.UI;

public sealed partial class MainWindow : Window
{
    private readonly INavigationService _navigationService;

    public MainWindow()
    {
        this.InitializeComponent();

        // Get navigation service from DI
        _navigationService = ServiceLocator.GetService<INavigationService>();
        _navigationService.SetFrame(ContentFrame);

        // Configure custom titlebar
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // Set the default page
        MainNavigationView.SelectedItem = MainNavigationView.MenuItems[0];
        _navigationService.NavigateTo<Views.DashboardPage>(NavigationTransitions.Suppress);
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
