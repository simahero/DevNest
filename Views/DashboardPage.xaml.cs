using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using DevNest.Controllers;

namespace DevNest.Views
{
    public sealed partial class DashboardPage : Page
    {
        private ServiceController _serviceController;

        public ServiceController ServiceController => _serviceController; public DashboardPage()
        {
            this.InitializeComponent();
            _serviceController = new ServiceController();

            this.DataContext = this;
            this.Loaded += DashboardPage_Loaded;
        }

        private async void DashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            await _serviceController.LoadServicesAsync();
        }

        private async void ServiceAction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ServiceInfo serviceInfo)
            {
                if (serviceInfo.IsRunning)
                {
                    await _serviceController.StopServiceAsync(serviceInfo);
                }
                else
                {
                    await _serviceController.StartServiceAsync(serviceInfo);
                }
            }
        }

        private async void StopAllServices_Click(object sender, RoutedEventArgs e)
        {
            await _serviceController.StopAllServicesAsync();
        }

        private async void RefreshServices_Click(object sender, RoutedEventArgs e)
        {
            await _serviceController.RefreshServiceStatusAsync();
        }
    }
}
