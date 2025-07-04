using DevNest.Core;
using DevNest.Core.Dump;
using DevNest.Core.Helpers;
using DevNest.Core.Managers;
using DevNest.Core.State;
using DevNest.UI.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.Threading.Tasks;


namespace DevNest.UI;

public partial class App : Application
{
    private Window? _window;
    private IHost? _host;
    public Window? Window => _window;

    public IServiceProvider? Services => _host?.Services;

    public App()
    {
        this.InitializeComponent();

        Application.Current.UnhandledException += (sender, e) =>
        {
            Debug.WriteLine($"Unhandled: {e.Exception}");
            Debug.WriteLine($"Message: {e.Exception.Message}");
            Debug.WriteLine($"Stack: {e.Exception.StackTrace}");
        };

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

        if (Services?.GetService(typeof(AppState)) is AppState appState)
        {
            _ = Task.Run(async () =>
            {
                await appState.LoadAsync();

                if (appState.Settings != null)
                {
                    PathHelper.SetUseWSL(appState.Settings.UseWLS);
                }

                if (Services?.GetService(typeof(SettingsManager)) is SettingsManager settingsManager)
                {
                    var autoSaveManager = new AutoSaveManager(appState, settingsManager);
                }
            });
        }


        if (Services?.GetService(typeof(VarDumperServer)) is VarDumperServer server)
        {
            _ = Task.Run(async () => await server.StartAsync());

        }
    }

}
