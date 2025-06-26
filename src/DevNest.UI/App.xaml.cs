using DevNest.Core.Interfaces;
using DevNest.Services;
using DevNest.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DevNest.UI;

public partial class App : Application
{
    private Window? _window;
    private IHost? _host;

    /// <summary>
    /// Gets the current window instance
    /// </summary>
    public Window? Window => _window;

    /// <summary>
    /// Gets the service provider
    /// </summary>
    public IServiceProvider? Services => _host?.Services;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
        ConfigureServices();
    }

    private void ConfigureServices()
    {
        _host = Host.CreateDefaultBuilder().ConfigureServices((context, services) =>
            {
                services.AddCoreServices();
                services.AddUIServices();
                services.AddSingleton<INavigationService, NavigationService>();

                // Register the current thread's DispatcherQueue for dependency injection
                services.AddSingleton(Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
                services.AddSingleton<IUIDispatcher, UIDispatcher>();
            })
            .Build();

        // Set the service provider for static access
        ServiceLocator.SetServiceProvider(_host.Services);
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Preload SVG logo resources to improve load speed
        var keys = new[]
        {
            "ApacheLogo", "MySqlLogo", "NginxLogo", "NodeLogo", "PhpLogo", "PhpMyAdminLogo", "PostgreSqlLogo", "RedisLogo"
        };
        foreach (var key in keys)
        {
            // Access the resource to force load
            var _ = Current.Resources[key];
        }

        // Ensure starter_dirs is present next to the EXE
        await CopyStarterDirOnStartup();
        _window = new MainWindow();
        _window.Activate();
    }

    private Task CopyStarterDirOnStartup()
    {
        LogManager? logger = Services?.GetService(typeof(LogManager)) as LogManager;
        IPathService? pathService = Services?.GetService(typeof(IPathService)) as IPathService;

        if (pathService == null)
        {
            logger?.Log("IPathService is not available.");
            return Task.CompletedTask;
        }

        string exePath = pathService.BasePath;

        // Try both possible locations for the source directory
        string[] possibleSources = new[]
        {
            Path.Combine(exePath, "Assets", "starter_dirs"), // For Debug or non-single-file
            Path.Combine(AppContext.BaseDirectory, "Assets", "starter_dirs") // For single-file publish
        };

        string? sourceDir = null;
        foreach (var src in possibleSources)
        {
            if (Directory.Exists(src))
            {
                sourceDir = src;
                break;
            }
        }

        logger?.Log($"Copying starter_dirs from {sourceDir ?? "<not found>"} to {exePath}");

        try
        {
            if (sourceDir != null)
            {
                CopyDirectory(sourceDir, exePath, overwrite: true);
            }
            else
            {
                logger?.Log($"Source starter_dirs not found in any known location.");
            }
        }
        catch (Exception ex)
        {
            logger?.Log($"Failed to copy starter_dirs: {ex.Message}");
        }
        return Task.CompletedTask;
    }

    private static void CopyDirectory(string sourceDir, string destDir, bool overwrite)
    {
        Directory.CreateDirectory(destDir);
        foreach (var dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, dirPath);
            var destPath = Path.Combine(destDir, relativePath);
            Directory.CreateDirectory(destPath);
        }

        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, file);
            var destFile = Path.Combine(destDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
            File.Copy(file, destFile, overwrite);
        }
    }
}
