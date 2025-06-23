using System.Collections.ObjectModel;
using System.ComponentModel;

namespace DevNest.Core.Models
{
    public class AppSettings : INotifyPropertyChanged
    {
        public bool StartWithWindows { get; set; } = false;
        public bool MinimizeToSystemTray { get; set; } = false;
        public bool AutoVirtualHosts { get; set; } = false;
        public bool AutoCreateDatabase { get; set; } = false;
        public string InstallDirectory { get; set; } = @"C:\DevNest";
        public ObservableCollection<ServiceVersion> Versions { get; set; } = new ObservableCollection<ServiceVersion>();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class ServiceVersion : INotifyPropertyChanged
    {
        private string _version = string.Empty;
        private ObservableCollection<string> _availableVersions = new();

        public required string Service { get; set; }
        public string Version
        {
            get => _version;
            set
            {
                if (_version != value)
                {
                    _version = value;
                    OnPropertyChanged(nameof(Version));
                }
            }
        }

        public ObservableCollection<string> AvailableVersions
        {
            get => _availableVersions;
            set
            {
                if (_availableVersions != value)
                {
                    _availableVersions = value;
                    OnPropertyChanged(nameof(AvailableVersions));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
