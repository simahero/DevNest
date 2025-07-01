using System.ComponentModel;
using System.Diagnostics;
using DevNest.Core.Enums;

namespace DevNest.Core.Models
{

    public class ServiceModel : INotifyPropertyChanged
    {
        private ServiceStatus _status;
        private bool _isLoading;
        private Process? _process;
        private bool _isSelected = false;
        private string _path = string.Empty;

        public required string Name { get; set; }
        public required string DisplayName { get; set; }
        public required string Command { get; set; }
        public ServiceType ServiceType { get; set; }

        public string Path
        {
            get => _path;
            set
            {
                if (_path != value)
                {
                    _path = value;
                    OnPropertyChanged(nameof(Path));
                }
            }
        }

        public ServiceStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(IsRunning));
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }

        public Process? Process
        {
            get => _process;
            set
            {
                _process = value;
                OnPropertyChanged(nameof(Process));
                OnPropertyChanged(nameof(ProcessId));
            }
        }

        public string ProcessId => Process?.Id.ToString() ?? "N/A";

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public bool IsRunning => Status == ServiceStatus.Running;

        public string? WorkingDirectory { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ServiceDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ServiceType ServiceType { get; set; }
        public bool HasAdditionalDir { get; set; } = false;
        public string DisplayName => $"{Name} - {Description}";
    }
}
