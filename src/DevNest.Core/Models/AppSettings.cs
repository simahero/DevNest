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
        public ObservableCollection<CategoryVersion> Versions { get; set; } = new ObservableCollection<CategoryVersion>();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class CategoryVersion : INotifyPropertyChanged
    {
        private string _version = string.Empty;

        public required string Category { get; set; }
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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
