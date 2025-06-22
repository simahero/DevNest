using DevNest.Controllers;
using DevNest.Readers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static DevNest.Readers.SitesReader;

namespace DevNest.Views
{
    public sealed partial class SitesPage : Page
    {
        private readonly ObservableCollection<InstalledSite> _installedSites = new();
        private readonly SitesReader _sitesReader;
        private readonly DownloadController _downloadController;
        private readonly VirtualHostController _virtualHostController; public SitesPage()
        {
            this.InitializeComponent();

            _sitesReader = new SitesReader();
            _downloadController = new DownloadController();
            _virtualHostController = new VirtualHostController();

            LoadAvailableSiteTypes();
            LoadInstalledSites();

        }
        private async void LoadAvailableSiteTypes()
        {
            try
            {
                var siteTypes = await _sitesReader.LoadSiteConfiguration();

                SiteTypeComboBox.ItemsSource = siteTypes;
                SiteTypeComboBox.DisplayMemberPath = "Name";

                if (siteTypes.Count > 0)
                {
                    SiteTypeComboBox.PlaceholderText = $"Select from {siteTypes.Count} available site types...";
                }
                else
                {
                    SiteTypeComboBox.PlaceholderText = "No site types available";
                }
            }
            catch (Exception ex)
            {
                SiteTypeComboBox.PlaceholderText = $"Error loading site types: {ex.Message}";
            }
        }

        private void LoadInstalledSites()
        {

            try
            {
                _installedSites.Clear();
                var allSites = _sitesReader.LoadSites();

                foreach (var site in allSites)
                {
                    _installedSites.Add(site);
                }

                if (_installedSites.Any())
                {
                    NoSitesText.Visibility = Visibility.Collapsed;
                    SitesContainer.Visibility = Visibility.Visible;
                    LoadSitesUI(allSites);
                }
                else
                {
                    NoSitesText.Visibility = Visibility.Visible;
                    SitesContainer.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                NoSitesText.Text = $"Error loading installed services: {ex.Message}";
            }
        }

        private void LoadSitesUI(System.Collections.Generic.List<InstalledSite> sites)
        {
            SitesContainer.Children.Clear();

            foreach (var site in sites)
            {
                var siteCart = CreateSiteCard(site);
                SitesContainer.Children.Add(siteCart);
            }
        }

        private async void AddSiteButton_Click(object sender, RoutedEventArgs e)
        {
            if (SiteTypeComboBox.SelectedItem is not SiteType selectedSite)
            {
                return;
            }

            if (SiteNameTextBox.Text == "")
            {
                return;
            }

            try
            {
                AddSiteButton.IsEnabled = false;
                InstallationPanel.Visibility = Visibility.Visible;
                InstallationProgressBar.Visibility = Visibility.Visible;
                var progress = new Progress<string>(status =>
                {
                    InstallationStatusText.Text = status;
                });

                var sitePath = Path.Combine(@"C:\DevNest\www", SiteNameTextBox.Text);

                if (!Directory.Exists(sitePath))
                {
                    Directory.CreateDirectory(sitePath);
                }

                if (selectedSite.InstallType.ToLower() == "none")
                {
                    InstallationStatusText.Text = $"{selectedSite.Name} does not require installation.";

                    // Create virtual host if auto virtual hosts is enabled
                    await CreateVirtualHostIfEnabled(SiteNameTextBox.Text, progress);

                    return;
                }

                if (selectedSite.InstallType.ToLower() == "command")
                {
                    InstallationStatusText.Text = $"Installing {selectedSite.Name}...";
                    await RunInstallCommand(selectedSite.Command, selectedSite.Name, sitePath, progress);
                    InstallationStatusText.Text = $"{selectedSite.Name} installed successfully!";

                    // Create virtual host if auto virtual hosts is enabled
                    await CreateVirtualHostIfEnabled(SiteNameTextBox.Text, progress);

                    InstallationProgressBar.Visibility = Visibility.Collapsed;
                    return;
                }

                if (selectedSite.InstallType.ToLower() == "download")
                {


                    var tempFilePath = await _downloadController.DownloadToTempAsync(selectedSite.Url, progress);
                    await _downloadController.ExtractAsync(tempFilePath, sitePath, selectedSite.HasAdditionalDir, progress);

                    InstallationStatusText.Text = $"{selectedSite.Name} downloaded and installed successfully!";

                    // Create virtual host if auto virtual hosts is enabled
                    await CreateVirtualHostIfEnabled(SiteNameTextBox.Text, progress);

                    InstallationProgressBar.Visibility = Visibility.Collapsed;
                    return;
                }
            }
            catch (Exception ex)
            {
                InstallationStatusText.Text = $"Installation failed: {ex.Message}";
                InstallationProgressBar.Visibility = Visibility.Collapsed;
            }
            finally
            {
                LoadInstalledSites();
                SiteTypeComboBox.SelectedItem = null;
                AddSiteButton.IsEnabled = true;
                await Task.Delay(3000);
                InstallationPanel.Visibility = Visibility.Collapsed;
            }

        }

        private async Task CreateVirtualHostIfEnabled(string siteName, IProgress<string>? progress = null)
        {
            var settings = AppSettingsController.Instance;
            if (settings.AutoVirtualHosts)
            {
                try
                {
                    progress?.Report("Creating virtual host...");
                    await _virtualHostController.CreateVirtualHostAsync(siteName);
                    progress?.Report($"Virtual host created: {siteName}.dev");
                }
                catch (Exception ex)
                {
                    progress?.Report($"Virtual host creation failed: {ex.Message}");
                    // Don't throw exception as this is not critical for site installation
                }
            }
        }

        private async Task RunInstallCommand(string command, string siteName, string sitePath, IProgress<string>? progress = null)
        {

            string actualCommand = command.Replace("%s", siteName);

            progress?.Report($"Running: {actualCommand}");

            var processInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c cd /d \"{System.IO.Path.GetDirectoryName(sitePath)}\" && {actualCommand}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            progress?.Report("Starting installation process...");

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                progress?.Report("Installing dependencies and setting up project...");

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    string error = await process.StandardError.ReadToEndAsync();
                    throw new Exception($"Command failed with exit code {process.ExitCode}: {error}");
                }

                progress?.Report("Installation completed successfully!");
            }
        }


        private void OpenSiteFolder(string siteName)
        {
            string wwwPath = @"C:\DevNest\www";
            string sitePath = System.IO.Path.Combine(wwwPath, siteName);
            if (Directory.Exists(sitePath))
            {
                Process.Start("explorer.exe", sitePath);
            }
        }

        private void OpenInVSCode(string siteName)
        {
            string wwwPath = @"C:\DevNest\www";
            string sitePath = System.IO.Path.Combine(wwwPath, siteName);
            if (Directory.Exists(sitePath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "code",
                        Arguments = $"\"{sitePath}\"",
                        UseShellExecute = true
                    });
                }
                catch
                {
                    // If VS Code is not in PATH, try common installation paths
                    string[] vscPaths = {
                        @"C:\Users\" + Environment.UserName + @"\AppData\Local\Programs\Microsoft VS Code\Code.exe",
                        @"C:\Program Files\Microsoft VS Code\Code.exe",
                        @"C:\Program Files (x86)\Microsoft VS Code\Code.exe"
                    };

                    foreach (string vscPath in vscPaths)
                    {
                        if (File.Exists(vscPath))
                        {
                            Process.Start(vscPath, $"\"{sitePath}\"");
                            return;
                        }
                    }
                }
            }
        }

        private void OpenInTerminal(string siteName)
        {
            string wwwPath = @"C:\DevNest\www";
            string sitePath = System.IO.Path.Combine(wwwPath, siteName);

            if (Directory.Exists(sitePath))
            {
                try
                {
                    // Try Windows Terminal first (modern terminal)
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "wt.exe",
                        Arguments = $"-d \"{sitePath}\"",
                        UseShellExecute = true
                    });
                }
                catch
                {
                    try
                    {
                        // Fallback to PowerShell
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            Arguments = $"-NoExit -Command \"Set-Location '{sitePath}'\"",
                            UseShellExecute = true
                        });
                    }
                    catch
                    {
                        // Final fallback to Command Prompt
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/k cd /d \"{sitePath}\"",
                            UseShellExecute = true
                        });
                    }
                }
            }
        }

        private void OpenInBrowser(string siteName)
        {
            string wwwPath = @"C:\DevNest\www";
            string sitePath = System.IO.Path.Combine(wwwPath, siteName);
            if (Directory.Exists(sitePath))
            {
                string indexPath = System.IO.Path.Combine(sitePath, "index.html");
                if (File.Exists(indexPath))
                {
                    string url = $"file:///{indexPath.Replace('\\', '/')}";
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                else
                {
                    // Open the folder in browser if no index.html
                    string url = $"file:///{sitePath.Replace('\\', '/')}";
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
            }
        }

        private Border CreateSiteCard(InstalledSite site)
        {
            var border = new Border
            {
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 12)
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Header row with site name
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(headerGrid, 0);

            var leftStackPanel = new StackPanel();
            Grid.SetColumn(leftStackPanel, 0);

            var siteNameBlock = new TextBlock
            {
                Text = site.Name,
                Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"]
            };
            leftStackPanel.Children.Add(siteNameBlock);

            var pathBlock = new TextBlock
            {
                Text = $"C:/DevNest/www/{site.Name}",
                Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            };

            leftStackPanel.Children.Add(pathBlock);
            headerGrid.Children.Add(leftStackPanel);

            // Info row
            var infoGrid = new Grid
            {
                Margin = new Thickness(0, 8, 0, 0)
            };
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(infoGrid, 1);

            var buttonsStackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 12, 0, 0)
            };
            Grid.SetRow(buttonsStackPanel, 2);

            var openButton = new Button
            {
                Content = new Microsoft.UI.Xaml.Controls.ImageIcon
                {
                    Source = new Microsoft.UI.Xaml.Media.Imaging.SvgImageSource(new Uri("ms-appx:///Assets/Icons/folder-open.svg")),
                    Width = 16,
                    Height = 16,
                },
                Margin = new Thickness(0, 0, 8, 0),
                Padding = new Thickness(8)
            };
            openButton.Click += (s, e) => OpenSiteFolder(site.Name);

            var vscodeButton = new Button
            {
                Content = new Microsoft.UI.Xaml.Controls.ImageIcon
                {
                    Source = new Microsoft.UI.Xaml.Media.Imaging.SvgImageSource(new Uri("ms-appx:///Assets/Icons/vscode.svg")),
                    Width = 16,
                    Height = 16,
                },
                Margin = new Thickness(0, 0, 8, 0),
                Padding = new Thickness(8)
            };
            vscodeButton.Click += (s, e) => OpenInVSCode(site.Name);

            var terminalButton = new Button
            {
                Content = new Microsoft.UI.Xaml.Controls.ImageIcon
                {
                    Source = new Microsoft.UI.Xaml.Media.Imaging.SvgImageSource(new Uri("ms-appx:///Assets/Icons/terminal.svg")),
                    Width = 16,
                    Height = 16,
                },
                Margin = new Thickness(0, 0, 8, 0),
                Padding = new Thickness(8)
            };
            terminalButton.Click += (s, e) => OpenInTerminal(site.Name);

            var browserButton = new Button
            {
                Content = new Microsoft.UI.Xaml.Controls.ImageIcon
                {
                    Source = new Microsoft.UI.Xaml.Media.Imaging.SvgImageSource(new Uri("ms-appx:///Assets/Icons/link.svg")),
                    Width = 16,
                    Height = 16,
                },
                Margin = new Thickness(0, 0, 8, 0),
                Padding = new Thickness(8)
            };
            browserButton.Click += (s, e) => OpenInBrowser(site.Name);

            var settingsButton = new Button
            {
                Content = "Settings"
            };

            buttonsStackPanel.Children.Add(openButton);
            buttonsStackPanel.Children.Add(vscodeButton);
            buttonsStackPanel.Children.Add(terminalButton);
            buttonsStackPanel.Children.Add(browserButton);
            buttonsStackPanel.Children.Add(settingsButton);

            mainGrid.Children.Add(headerGrid);
            mainGrid.Children.Add(infoGrid);
            mainGrid.Children.Add(buttonsStackPanel);

            border.Child = mainGrid;
            return border;
        }
    }
}
