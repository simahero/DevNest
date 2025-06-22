using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using System;
using Microsoft.UI.Xaml;
using DevNest.Controllers;

namespace DevNest.Views
{
    public sealed partial class SettingsPage : Page
    {
        public AppSettingsController Settings => AppSettingsController.Instance;

        public SettingsPage()
        {
            this.InitializeComponent();
            LoadSettings();
        }
        private void LoadSettings()
        {
            StartWithWindowsToggle.IsOn = Settings.StartWithWindows;
            MinimizeToTrayToggle.IsOn = Settings.MinimizeToSystemTray;
            AutoVirtualHostsToggle.IsOn = Settings.AutoVirtualHosts;
            AutoCreateDatabaseToggle.IsOn = Settings.AutoCreateDatabase;
            InstallDirectoryTextBox.Text = Settings.InstallDirectory;

            StartWithWindowsToggle.Toggled += OnSettingChanged;
            MinimizeToTrayToggle.Toggled += OnSettingChanged;
            AutoVirtualHostsToggle.Toggled += OnSettingChanged;
            AutoCreateDatabaseToggle.Toggled += OnSettingChanged;
        }

        private void OnSettingChanged(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        private void SaveSettings()
        {
            // Update settings from UI controls
            Settings.StartWithWindows = StartWithWindowsToggle.IsOn;
            Settings.MinimizeToSystemTray = MinimizeToTrayToggle.IsOn;
            Settings.AutoVirtualHosts = AutoVirtualHostsToggle.IsOn;
            Settings.AutoCreateDatabase = AutoCreateDatabaseToggle.IsOn;
            Settings.InstallDirectory = InstallDirectoryTextBox.Text;

            // Save to file
            Settings.Save();
        }

        private async void BrowseInstallDirectory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FolderPicker();
                picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
                picker.FileTypeFilter.Add("*");

                // Get the window handle using the app's main window
                var window = (Application.Current as App)?.Window;
                if (window != null)
                {
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                    var folder = await picker.PickSingleFolderAsync();
                    if (folder != null)
                    {
                        InstallDirectoryTextBox.Text = folder.Path;
                        SaveSettings(); // Save the new directory immediately
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle any errors silently for now
                System.Diagnostics.Debug.WriteLine($"Error picking folder: {ex.Message}");
            }
        }
    }
}
