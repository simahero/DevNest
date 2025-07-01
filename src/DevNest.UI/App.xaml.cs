using DevNest.Core.Dump;
using DevNest.Core.Interfaces;
using DevNest.UI.Services;
using DevNest.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using System;
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
        ConfigureServices();

        // Get the VarDumperServer from DI container and start it
        if (_host != null)
        {
            var server = _host.Services.GetRequiredService<VarDumperServer>();
            _ = Task.Run(async () => await server.StartAsync());
        }
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
            var _ = Current.Resources[key];
        }

        _window = new MainWindow();
        _window.Activate();
    }

}
