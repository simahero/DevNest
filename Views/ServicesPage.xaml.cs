using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;
using DevNest.Readers;
using DevNest.Controllers;

namespace DevNest.Views
{
    public sealed partial class ServicesPage : Page
    {
        private readonly ObservableCollection<InstalledService> _installedServices = new();
        private readonly ServicesReader _ServicesReader;
        private readonly DownloadController _downloadController;

        public ServicesPage()
        {
            this.InitializeComponent();

            _ServicesReader = new ServicesReader();
            _downloadController = new DownloadController();

            LoadAvailableServices();
            LoadInstalledServices();

            // LoadAndCompareServices();
        }
        private async void LoadAvailableServices()
        {
            try
            {
                var services = await _ServicesReader.LoadServiceConfigurationAsync();

                ServiceComboBox.ItemsSource = services;
                ServiceComboBox.DisplayMemberPath = "DisplayName";

                if (services.Count > 0)
                {
                    ServiceComboBox.PlaceholderText = $"Select from {services.Count} available services...";
                }
                else
                {
                    ServiceComboBox.PlaceholderText = "No services available";
                }
            }
            catch (Exception ex)
            {
                ServiceComboBox.PlaceholderText = $"Error loading services: {ex.Message}";
            }
        }

        private void LoadInstalledServices()
        {
            try
            {
                _installedServices.Clear();
                var allServices = _ServicesReader.LoadInstalledServices();

                foreach (var service in allServices)
                {
                    _installedServices.Add(service);
                }

                if (_installedServices.Any())
                {
                    NoServicesText.Visibility = Visibility.Collapsed;
                    ServicesContainer.Visibility = Visibility.Visible;
                    LoadServicesUI(allServices);
                }
                else
                {
                    NoServicesText.Visibility = Visibility.Visible;
                    ServicesContainer.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                NoServicesText.Text = $"Error loading installed services: {ex.Message}";
            }
        }

        private void LoadServicesUI(List<InstalledService> services)
        {
            ServicesContainer.Children.Clear();

            foreach (var service in services)
            {
                var serviceCard = CreateServiceCard(service);
                ServicesContainer.Children.Add(serviceCard);
            }
        }

        private Border CreateServiceCard(InstalledService service)
        {
            var border = new Border
            {
                Background = Application.Current.Resources["CardBackgroundFillColorDefaultBrush"] as Microsoft.UI.Xaml.Media.Brush,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 12)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var stackPanel = new StackPanel();

            var nameText = new TextBlock
            {
                Text = service.Name,
                Style = Application.Current.Resources["BodyStrongTextBlockStyle"] as Style,
                Margin = new Thickness(0, 0, 0, 4)
            };

            var pathText = new TextBlock
            {
                Text = service.Path,
                Style = Application.Current.Resources["CaptionTextBlockStyle"] as Style,
                Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"] as Microsoft.UI.Xaml.Media.Brush
            };

            stackPanel.Children.Add(nameText);
            stackPanel.Children.Add(pathText);
            var openButton = new Button
            {
                Content = new Microsoft.UI.Xaml.Controls.ImageIcon
                {
                    Source = new Microsoft.UI.Xaml.Media.Imaging.SvgImageSource(new Uri("ms-appx:///Assets/Icons/folder-open.svg")),
                    Width = 16,
                    Height = 16
                },
                Padding = new Thickness(4),
                Style = Application.Current.Resources["DefaultButtonStyle"] as Style,
                Tag = service.Path
            };
            openButton.Click += OpenFolder_Click;

            Grid.SetColumn(stackPanel, 0);
            Grid.SetColumn(openButton, 1);

            grid.Children.Add(stackPanel);
            grid.Children.Add(openButton);

            border.Child = grid;
            return border;
        }

        private void ServiceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            InstallButton.IsEnabled = ServiceComboBox.SelectedItem != null;
        }

        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (ServiceComboBox.SelectedItem is not ServiceItem selectedService)
            {
                return;
            }

            try
            {
                InstallButton.IsEnabled = false;
                InstallationPanel.Visibility = Visibility.Visible;
                InstallationProgressBar.Visibility = Visibility.Visible;
                var progress = new Progress<string>(status =>
                {
                    InstallationStatusText.Text = status;
                });

                // Download the service to temp directory
                var tempFilePath = await _downloadController.DownloadToTempAsync(selectedService.Url, progress);

                // Create service directory path
                var categoryPath = Path.Combine(@"C:\DevNest\bin", selectedService.Category);
                var servicePath = Path.Combine(categoryPath, selectedService.Name);

                // Ensure category directory exists
                if (!Directory.Exists(categoryPath))
                {
                    Directory.CreateDirectory(categoryPath);
                }

                // Extract service to its directory
                await _downloadController.ExtractAsync(tempFilePath, servicePath, selectedService.HasAdditionalDir, progress);

                // Clean up temp file
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }

                InstallationStatusText.Text = $"{selectedService.Name} installed successfully!";
                InstallationProgressBar.Visibility = Visibility.Collapsed;

                // Refresh installed services list
                LoadInstalledServices();

                // Reset selection
                ServiceComboBox.SelectedItem = null;
            }
            catch (Exception ex)
            {
                InstallationStatusText.Text = $"Installation failed: {ex.Message}";
                InstallationProgressBar.Visibility = Visibility.Collapsed;
            }
            finally
            {
                InstallButton.IsEnabled = true;

                // Hide installation panel after 3 seconds
                await Task.Delay(3000);
                InstallationPanel.Visibility = Visibility.Collapsed;
            }
        }

        private async void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string folderPath)
            {
                try
                {
                    await Launcher.LaunchFolderPathAsync(folderPath);
                }
                catch (Exception ex)
                {
                    // Handle error - maybe show a content dialog
                    var dialog = new ContentDialog
                    {
                        Title = "Error",
                        Content = $"Failed to open folder: {ex.Message}",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await dialog.ShowAsync();
                }
            }
        }

    }
}
