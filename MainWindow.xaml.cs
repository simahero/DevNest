using DevNest.Readers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using WinRT.Interop;
using static DevNest.Readers.SitesReader;

namespace DevNest
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private bool _isClosing = false;
        private List<string> _sites = new List<string>();
        private SystemTrayHelper? _trayHelper;
        private WindowSubclass? _windowSubclass;
        private readonly SitesReader _sitesReader;

        public MainWindow()
        {
            _sitesReader = new SitesReader();
            InitializeComponent();

            // Configure custom titlebar
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            // Set window icon
            SetWindowIcon();

            // Set the default page
            MainNavigationView.SelectedItem = MainNavigationView.MenuItems[0];
            ContentFrame.Navigate(typeof(Views.DashboardPage));

            // Handle window closing event
            this.Closed += MainWindow_Closed;

            // Initialize some sample sites
            _sites = new List<string> { "WordPress Blog", "Laravel API", "Symfony CMS" };

            // Initialize system tray after window is loaded
            this.Activated += MainWindow_Activated;
        }
        private void MainWindow_Closed(object sender, WindowEventArgs e)
        {
            if (!_isClosing)
            {
                // Prevent actual closing and hide the window instead
                e.Handled = true;
                HideWindow();
            }
        }

        // Method to hide the window using Win32 API
        private void HideWindow()
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            ShowWindow(hwnd, SW_HIDE);
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem selectedItem)
            {
                string? tag = selectedItem.Tag?.ToString();

                switch (tag)
                {
                    case "Dashboard":
                        ContentFrame.Navigate(typeof(Views.DashboardPage));
                        break;
                    case "Services":
                        ContentFrame.Navigate(typeof(Views.ServicesPage));
                        break;
                    case "Sites":
                        ContentFrame.Navigate(typeof(Views.SitesPage));
                        break;
                    case "Dumps":
                        ContentFrame.Navigate(typeof(Views.DumpsPage));
                        break;
                    case "Settings":
                        ContentFrame.Navigate(typeof(Views.SettingsPage));
                        break;
                }
            }
        }

        // Method to show the window (for future tray implementation)
        public void ShowMainWindow()
        {
            this.Activate();
            // Show the window if it's hidden
            var hwnd = WindowNative.GetWindowHandle(this);
            ShowWindow(hwnd, SW_RESTORE);
        }

        // Method to properly exit the application
        public void ExitApplication()
        {
            _isClosing = true;

            // Clean up system tray
            if (_trayHelper != null)
            {
                _trayHelper.RemoveTrayIcon();
                _trayHelper = null;
            }

            Application.Current.Exit();
        }        // Methods for tray context menu        
        public List<InstalledSite> GetSites()
        {
            return _sitesReader.LoadSites();
        }

        public void AddSite(string? siteType)
        {
            if (string.IsNullOrEmpty(siteType)) return;

            string siteName = $"New {siteType} Site {_sites.Count + 1}";
            _sites.Add(siteName);

            // Navigate to Sites page and show window
            MainNavigationView.SelectedItem = MainNavigationView.MenuItems[2]; // Sites is the 3rd item
            ContentFrame.Navigate(typeof(Views.SitesPage));
            ShowMainWindow();
        }
        public void ExploreSite(string siteName)
        {
            // Use the SitesReader to open the site folder
            //_sitesReader.OpenSiteFolder(siteName);

            // Navigate to Sites page and show window
            MainNavigationView.SelectedItem = MainNavigationView.MenuItems[2]; // Sites is the 3rd item
            ContentFrame.Navigate(typeof(Views.SitesPage));
            ShowMainWindow();
        }

        public void OpenSite(string siteName)
        {
            // Use the SitesReader to open the site in browser
            //_sitesReader.OpenInBrowser(siteName);
            ShowMainWindow();
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            // Initialize system tray on first activation
            if (_trayHelper == null)
            {
                InitializeSystemTray();
            }
        }

        private void InitializeSystemTray()
        {
            try
            {
                var hwnd = WindowNative.GetWindowHandle(this);
                _trayHelper = new SystemTrayHelper(this, hwnd);

                // Set up window subclassing to handle tray messages
                _windowSubclass = new WindowSubclass();
                _windowSubclass.InstallSubclass(hwnd, (h, msg, wParam, lParam) =>
                {
                    if (_trayHelper != null && _trayHelper.ProcessTrayIconMessage(h, (int)msg, wParam, lParam))
                    {
                        return IntPtr.Zero;
                    }
                    return WindowSubclass.DefSubclassProc(h, msg, wParam, lParam);
                });
            }
            catch (Exception ex)
            {
                // Log error but don't crash the app
                System.Diagnostics.Debug.WriteLine($"Failed to initialize system tray: {ex.Message}");
            }
        }

        private void SetWindowIcon()
        {
            try
            {
                var hwnd = WindowNative.GetWindowHandle(this);

                // Load the icon
                string iconPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "",
                    "Assets",
                    "Square44x44Logo.scale-200.png"
                );

                if (System.IO.File.Exists(iconPath))
                {
                    // For now, we'll use the default icon since PNG to ICO conversion requires additional work
                    // The icon in the titlebar will be shown via the XAML Image element we added

                    // Set the window icon using Win32 API (this would need an ICO file)
                    // For now, this is a placeholder - the visual icon in titlebar is handled by XAML
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to set window icon: {ex.Message}");
            }
        }

        // Win32 API for showing/hiding windows
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_HIDE = 0;
        private const int SW_RESTORE = 9;
    }
}
