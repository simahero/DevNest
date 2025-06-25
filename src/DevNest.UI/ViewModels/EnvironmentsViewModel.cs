using CommunityToolkit.Mvvm.ComponentModel;
using DevNest.Core.Models;
using DevNest.Services;
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

        public EnvironmentsViewModel(SettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
            Title = "Environments";
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
        }

        protected override async Task OnUnloadedAsync()
        {
            await base.OnUnloadedAsync();
        }

    }
}
