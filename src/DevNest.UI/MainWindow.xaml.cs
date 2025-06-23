using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DevNest.UI.Services;

namespace DevNest.UI;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
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
        _navigationService.NavigateTo<Views.DashboardPage>();
    }

    private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem selectedItem)
        {
            var tag = selectedItem.Tag?.ToString();
            switch (tag)
            {
                case "Dashboard":
                    _navigationService.NavigateTo<Views.DashboardPage>();
                    break;
                case "Services":
                    _navigationService.NavigateTo<Views.ServicesPage>();
                    break;
                case "Sites":
                    _navigationService.NavigateTo<Views.SitesPage>();
                    break;
                case "Dumps":
                    _navigationService.NavigateTo<Views.DumpsPage>();
                    break;
                case "Settings":
                    _navigationService.NavigateTo<Views.SettingsPage>();
                    break;
            }
        }
    }
}
