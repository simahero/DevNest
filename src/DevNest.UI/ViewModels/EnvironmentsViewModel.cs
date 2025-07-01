using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevNest.Core;
using DevNest.Core.Files;
using DevNest.Core.Models;
using DevNest.UI.Services;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace DevNest.UI.ViewModels
{
    public partial class EnvironmentsViewModel : BaseViewModel
    {
        private readonly SettingsManager _settingsManager;

        [ObservableProperty]
        private ApacheSettings? _apache;

        [ObservableProperty]
        private MySQLSettings? _mySQL;

        [ObservableProperty]
        private PHPSettings? _php;

        [ObservableProperty]
        private NodeSettings? _node;

        [ObservableProperty]
        private RedisSettings? _redis;

        [ObservableProperty]
        private PostgreSQLSettings? _postgreSQL;

        [ObservableProperty]
        private NginxSettings? _nginx;

        [ObservableProperty]
        private MongoDBSettings? _mongoDB;

        public EnvironmentsViewModel(SettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
            Title = "Environments";
        }

        [RelayCommand]
        private void OpenSettings()
        {
            var pathManager = ServiceLocator.GetService<PathManager>();
            var settingsPath = Path.Combine(pathManager.ConfigPath, "settings.ini");
            if (File.Exists(settingsPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = settingsPath,
                    UseShellExecute = true
                });
            }
        }

        [RelayCommand]
        private void OpenEtc()
        {
            var pathManager = ServiceLocator.GetService<PathManager>();
            var etcPath = Path.Combine(pathManager.EtcPath);
            if (Directory.Exists(etcPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = etcPath,
                    UseShellExecute = true
                });
            }
        }

        public override async Task LoadAsync()
        {
            await base.LoadAsync();
            var settings = await _settingsManager.LoadSettingsAsync(false);

            Apache = settings.Apache;
            MySQL = settings.MySQL;
            Php = settings.PHP;
            Node = settings.Node;
            Redis = settings.Redis;
            PostgreSQL = settings.PostgreSQL;
            Nginx = settings.Nginx;
            MongoDB = settings.MongoDB;
        }

        protected override async Task OnUnloadedAsync()
        {
            await base.OnUnloadedAsync();
        }

    }
}
