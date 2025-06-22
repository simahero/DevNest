using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DevNest
{    /// <summary>
     /// Provides application-specific behavior to supplement the default Application class.
     /// </summary>
    public partial class App : Application
    {
        private Window? _window;

        /// <summary>
        /// Gets the current window instance
        /// </summary>
        public Window? Window => _window;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // Create required directories on first launch
            CreateRequiredDirectories();

            _window = new MainWindow();
            _window.Activate();
        }

        /// <summary>
        /// Creates the required directories for the application.
        /// </summary>
        private void CreateRequiredDirectories()
        {
            try
            {
                // Create DevNest directory in C:\ root
                string appBasePath = @"C:\DevNest";

                // Create the base DevNest directory if it doesn't exist
                if (!Directory.Exists(appBasePath))
                {
                    Directory.CreateDirectory(appBasePath);
                }

                string binPath = System.IO.Path.Combine(appBasePath, "bin");
                if (!Directory.Exists(binPath))
                {
                    Directory.CreateDirectory(binPath);
                }

                // Create data directory  
                string dataPath = System.IO.Path.Combine(appBasePath, "data");
                if (!Directory.Exists(dataPath))
                {
                    Directory.CreateDirectory(dataPath);
                }

                // Create www directory  
                string wwwPath = System.IO.Path.Combine(appBasePath, "www");
                if (!Directory.Exists(wwwPath))
                {
                    Directory.CreateDirectory(wwwPath);
                }

                // Create etc directory  
                string etcPath = System.IO.Path.Combine(appBasePath, "etc");
                if (!Directory.Exists(etcPath))
                {
                    Directory.CreateDirectory(etcPath);
                }

            }
            catch (Exception ex)
            {
                // Log the error or handle it appropriately
                // For now, we'll silently continue as directory creation failure
                // shouldn't prevent the app from starting
                System.Diagnostics.Debug.WriteLine($"Failed to create directories: {ex.Message}");
            }
        }
    }
}
