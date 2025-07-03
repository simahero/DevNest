using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace DevNest.Core.Models
{
    public partial class Model : ObservableObject
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

    public partial class ApacheModel : ObservableObject
    {
        [ObservableProperty]
        private string _version = "";

        [ObservableProperty]
        private int _port = 80;

        [ObservableProperty]
        private bool _autoStart = false;

        public ObservableCollection<string> AvailableVersions { get; set; } = new();
        public ObservableCollection<ServiceDefinition> InstallableVersions { get; set; } = new();
    }

    public partial class MySQLModel : ObservableObject
    {
        [ObservableProperty]
        private string _version = "";

        [ObservableProperty]
        private int _port = 3306;

        [ObservableProperty]
        private bool _autoStart = false;

        public ObservableCollection<string> AvailableVersions { get; set; } = new();
        public ObservableCollection<ServiceDefinition> InstallableVersions { get; set; } = new();
    }

    public partial class PHPModel : ObservableObject
    {
        [ObservableProperty]
        private string _version = "";

        public ObservableCollection<string> AvailableVersions { get; set; } = new();
        public ObservableCollection<ServiceDefinition> InstallableVersions { get; set; } = new();
    }

    public partial class NodeModel : ObservableObject
    {
        [ObservableProperty]
        private string _version = "";

        [ObservableProperty]
        private int _defaultPort = 3000;

        [ObservableProperty]
        private string _packageManager = "npm";

        [ObservableProperty]
        private bool _autoStart = false;

        public ObservableCollection<string> AvailableVersions { get; set; } = new();
        public ObservableCollection<ServiceDefinition> InstallableVersions { get; set; } = new();
    }

    public partial class RedisModel : ObservableObject
    {
        [ObservableProperty]
        private string _version = "";

        [ObservableProperty]
        private int _port = 6379;

        [ObservableProperty]
        private bool _autoStart = false;

        public ObservableCollection<string> AvailableVersions { get; set; } = new();
        public ObservableCollection<ServiceDefinition> InstallableVersions { get; set; } = new();
    }

    public partial class PostgreSQLModel : ObservableObject
    {
        [ObservableProperty]
        private string _version = "";

        [ObservableProperty]
        private int _port = 5432;

        [ObservableProperty]
        private bool _autoStart = false;

        public ObservableCollection<string> AvailableVersions { get; set; } = new();
        public ObservableCollection<ServiceDefinition> InstallableVersions { get; set; } = new();
    }

    public partial class NginxModel : ObservableObject
    {
        [ObservableProperty]
        private string _version = "";

        [ObservableProperty]
        private int _port = 8080;

        [ObservableProperty]
        private bool _autoStart = false;

        public ObservableCollection<string> AvailableVersions { get; set; } = new();
        public ObservableCollection<ServiceDefinition> InstallableVersions { get; set; } = new();
    }

    public partial class MongoDBModel : ObservableObject
    {
        [ObservableProperty]
        private string _version = "";

        [ObservableProperty]
        private int _port = 27017;

        [ObservableProperty]
        private bool _autoStart = false;

        public ObservableCollection<string> AvailableVersions { get; set; } = new();
        public ObservableCollection<ServiceDefinition> InstallableVersions { get; set; } = new();
    }

}
