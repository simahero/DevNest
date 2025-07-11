using DevNest.Core;
using DevNest.Core.Helpers;
using DevNest.Core.Managers;
using DevNest.Core.Managers.Dump;
using DevNest.Core.Managers.SMTP;
using DevNest.Core.State;
using DevNest.UI.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;


namespace DevNest.UI;

public partial class App : Application
{
    private Window? _window;
    private IHost? _host;
    private AutoSaveManager? _autoSaveManager;
    public Window? Window => _window;

    public IServiceProvider? Services => _host?.Services;

    public App()
    {
        this.InitializeComponent();

        // Application.Current.UnhandledException += (sender, e) =>
        // {
        //     Debug.WriteLine($"Unhandled: {e.Exception}");
        //     Debug.WriteLine($"Message: {e.Exception.Message}");
        //     Debug.WriteLine($"Stack: {e.Exception.StackTrace}");
        // };

        ConfigureServices();
    }

    private void ConfigureServices()
    {
        _host = Host.CreateDefaultBuilder().ConfigureServices((context, services) =>
            {
                services.AddCoreServices();
                services.AddUIServices();
            })
            .Build();

        ServiceLocator.SetServiceProvider(_host.Services);
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var keys = new[]
        {
            "ApacheLogo", "MySqlLogo", "NginxLogo", "NodeLogo", "PhpLogo", "PhpMyAdminLogo", "PostgreSqlLogo", "RedisLogo", "MongoLogo"
        };
        foreach (var key in keys)
        {
            _ = Current.Resources[key];
        }

        _window = new MainWindow();
        _window.Activate();

        if (Services != null)
        {
            _ = Task.Run(async () =>
            {
                var appState = Services.GetService(typeof(AppState)) as AppState;

                if (appState != null)
                {
                    await appState.LoadAsync();

                    if (appState.Settings != null)
                    {
                        PathHelper.SetUseWSL(appState.Settings.UseWSL);
                    }

                    if (Services.GetService(typeof(SettingsManager)) is SettingsManager settingsManager)
                    {
                        //var autoSaveManager = Services.GetService(typeof(AutoSaveManager)) as AutoSaveManager;
                        //// Create AutoSaveManager with 1000ms debounce delay and store as field to prevent GC
                        _autoSaveManager = new AutoSaveManager(appState, settingsManager, 0);
                    }
                }
            });
        }


        if (Services?.GetService(typeof(VarDumperServer)) is VarDumperServer server)
        {
            _ = Task.Run(async () => await server.StartAsync());

        }

        if (Services?.GetService(typeof(SMTP)) is SMTP smtp)
        {
            _ = Task.Run(async () => await smtp.StartAsync());

        }
    }

    // Properly dispose of AutoSaveManager when app shuts down
    public void DisposeAutoSaveManager()
    {
        _autoSaveManager?.Dispose();
        _autoSaveManager = null;
    }
}
