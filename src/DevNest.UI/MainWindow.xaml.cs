using DevNest.Core;
using DevNest.UI.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevNest.UI;

public sealed partial class MainWindow : Window
{
    private readonly INavigationService _navigationService;
    private readonly StartupManager _startupManager;

    private User32Dll _user32Dll;

    public MainWindow()
    {
        this.InitializeComponent();

        _startupManager = ServiceLocator.GetService<StartupManager>();
        _navigationService = ServiceLocator.GetService<INavigationService>();
        _navigationService.SetFrame(ContentFrame);

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // MainNavigationView.SelectedItem = MainNavigationView.MenuItems[0];
        _navigationService.NavigateTo<Views.DashboardPage>(NavigationTransitions.Suppress);

        _user32Dll = new User32Dll();
        _user32Dll.InitializeTrayIcon(this);

        this.Closed += MainWindow_Closed;

    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        // Prevent the window from actually closing and hide it instead
        args.Handled = true;

        // Hide the window to system tray
        _user32Dll.HideWindow();
    }

    public void RestoreFromTray()
    {
        _user32Dll.ShowWindow();
        this.Activate();
    }

    public void ActuallyExit()
    {
        _user32Dll.RemoveTrayIcon();
        Application.Current.Exit();
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
