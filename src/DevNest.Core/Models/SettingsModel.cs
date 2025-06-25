using CommunityToolkit.Mvvm.ComponentModel;
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

        public ApacheSettings Apache { get; set; } = new ApacheSettings();
        public MySQLSettings MySQL { get; set; } = new MySQLSettings();
        public PHPSettings PHP { get; set; } = new PHPSettings();
        public NodeSettings Node { get; set; } = new NodeSettings();
        public RedisSettings Redis { get; set; } = new RedisSettings();
        public PostgreSQLSettings PostgreSQL { get; set; } = new PostgreSQLSettings();
        public NginxSettings Nginx { get; set; } = new NginxSettings();
    }

    public partial class ApacheSettings : ObservableObject
    {
        [ObservableProperty]
        private string _version = "";

        [ObservableProperty]
        private string _documentRoot = @"C:\DevNest\www";

        [ObservableProperty]
        private int _listenPort = 80;

        [ObservableProperty]
        private bool _autoStart = false;

        [ObservableProperty]
        private string _logLevel = "Info";

        public ObservableCollection<string> AvailableVersions { get; set; } = new();
    }

    public partial class MySQLSettings : ObservableObject
    {
        [ObservableProperty]
        private string _version = "";

        [ObservableProperty]
        private int _port = 3306;

        [ObservableProperty]
        private string _rootPassword = "";

        [ObservableProperty]
        private bool _autoStart = false;

        [ObservableProperty]
        private string _logLevel = "Info";

        public ObservableCollection<string> AvailableVersions { get; set; } = new();
    }

    public partial class PHPSettings : ObservableObject
    {
        [ObservableProperty]
        private string _version = "";

        [ObservableProperty]
        private string _memoryLimit = "256M";

        [ObservableProperty]
        private int _maxExecutionTime = 30;

        [ObservableProperty]
        private bool _autoStart = true;

        [ObservableProperty]
        private string _logLevel = "Info";

        public ObservableCollection<string> AvailableVersions { get; set; } = new();
    }

    public partial class NodeSettings : ObservableObject
    {
        [ObservableProperty]
        private string _version = "";

        [ObservableProperty]
        private int _defaultPort = 3000;

        [ObservableProperty]
        private string _packageManager = "npm";

        [ObservableProperty]
        private bool _autoStart = false;

        [ObservableProperty]
        private string _logLevel = "Info";

        public ObservableCollection<string> AvailableVersions { get; set; } = new();
    }

    public partial class RedisSettings : ObservableObject
    {
        [ObservableProperty]
        private string _version = "";

        [ObservableProperty]
        private bool _autoStart = false;

        [ObservableProperty]
        private string _logLevel = "Info";

        public ObservableCollection<string> AvailableVersions { get; set; } = new();
    }

    public partial class PostgreSQLSettings : ObservableObject
    {
        [ObservableProperty]
        private string _version = "";

        [ObservableProperty]
        private bool _autoStart = false;

        [ObservableProperty]
        private string _logLevel = "Info";

        public ObservableCollection<string> AvailableVersions { get; set; } = new();
    }

    public partial class NginxSettings : ObservableObject
    {
        [ObservableProperty]
        private string _version = "";

        [ObservableProperty]
        private bool _autoStart = false;

        [ObservableProperty]
        private string _logLevel = "Info";

        public ObservableCollection<string> AvailableVersions { get; set; } = new();
    }

}
