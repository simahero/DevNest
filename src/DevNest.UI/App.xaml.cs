using DevNest.Core.Interfaces;
using DevNest.Services;
using DevNest.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using System;

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
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
