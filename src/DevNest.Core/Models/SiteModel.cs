using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DevNest.Core.Models
{
    public class SiteModel : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _path = string.Empty;
        private string _url = string.Empty;
        private bool _isInstalled;
        private bool _isActive;
        private DateTime _createdDate;
        private Process? _shareProcess;

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Path
        {
            get => _path;
            set
            {
                if (_path != value)
                {
                    _path = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Url
        {
            get => _url;
            set
            {
                if (_url != value)
                {
                    _url = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsInstalled
        {
            get => _isInstalled;
            set
            {
                if (_isInstalled != value)
                {
                    _isInstalled = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime CreatedDate
        {
            get => _createdDate;
            set
            {
                if (_createdDate != value)
                {
                    _createdDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public Process? ShareProcess
        {
            get => _shareProcess;
            set
            {
                if (_shareProcess != value)
                {
                    _shareProcess = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSharing => ShareProcess != null && !ShareProcess.HasExited;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SiteDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string InstallType { get; set; } = string.Empty;
        public string InstallUrl { get; set; } = string.Empty;
        public string InstallCommand { get; set; } = string.Empty;
        public bool HasAdditionalDir { get; set; } = false;

    }

}
