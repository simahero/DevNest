using CommunityToolkit.Mvvm.ComponentModel;
using DevNest.Core.Helpers;
using System.Collections.ObjectModel;

namespace DevNest.Core.Models
{
    public partial class SettingsModel : ObservableObject
    {
        [ObservableProperty]
        private bool _startWithWindows = false;

        [ObservableProperty]
        private bool _minimizeToSystemTray = false;

        [ObservableProperty]
        private bool _autoVirtualHosts = false;

        [ObservableProperty]
        private bool _autoCreateDatabase = false;

        [ObservableProperty]
        private string _ngrokDomain = string.Empty;

        [ObservableProperty]
        private string _ngrokApiKey = string.Empty;

        [ObservableProperty]
        private bool _useWLS = false;

        public ApacheModel Apache { get; set; } = new ApacheModel();
        public MySQLModel MySQL { get; set; } = new MySQLModel();
        public PHPModel PHP { get; set; } = new PHPModel();
        public NodeModel Node { get; set; } = new NodeModel();
        public RedisModel Redis { get; set; } = new RedisModel();
        public PostgreSQLModel PostgreSQL { get; set; } = new PostgreSQLModel();
        public NginxModel Nginx { get; set; } = new NginxModel();
        public MongoDBModel MongoDB { get; set; } = new MongoDBModel();
    }

    public partial class ServiceSettingsModel : ObservableObject
    {
        [ObservableProperty]
        private string _version = "";

        [ObservableProperty]
        private int _port;

        [ObservableProperty]
        private bool _autoStart = false;

        public ObservableCollection<string> AvailableVersions { get; set; } = new();
        public ObservableCollection<ServiceDefinition> InstallableVersions { get; set; } = new();
    }

    public partial class ApacheModel : ServiceSettingsModel
    {
        public ApacheModel()
        {
            Port = 80;
        }
    }

    public partial class MySQLModel : ServiceSettingsModel
    {
        public MySQLModel()
        {
            Port = 3306;
        }
    }

    public partial class PHPModel : ServiceSettingsModel
    {
        public PHPModel()
        {
            Port = 0;
        }
    }

    public partial class NodeModel : ServiceSettingsModel
    {
        [ObservableProperty]
        private int _defaultPort = 3000;

        [ObservableProperty]
        private string _packageManager = "npm";

        public NodeModel()
        {
            Port = 3000;
        }
    }

    public partial class RedisModel : ServiceSettingsModel
    {
        public RedisModel()
        {
            Port = 6379;
        }
    }

    public partial class PostgreSQLModel : ServiceSettingsModel
    {
        public PostgreSQLModel()
        {
            Port = 5432;
        }
    }

    public partial class NginxModel : ServiceSettingsModel
    {
        public NginxModel()
        {
            Port = 8080;
        }
    }

    public partial class MongoDBModel : ServiceSettingsModel
    {
        public MongoDBModel()
        {
            Port = 27017;
        }
    }

}
