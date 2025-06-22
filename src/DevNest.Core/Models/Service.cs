using System.ComponentModel;
using System.Diagnostics;

namespace DevNest.Core.Models
{
    public enum ServiceStatus
    {
        Stopped,
        Running,
        Starting,
        Stopping
    }

    public class Service : INotifyPropertyChanged
    {
        private ServiceStatus _status;
        private bool _isLoading;
        private Process? _process;

        public required string Name { get; set; }
        public required string DisplayName { get; set; }
        public required string Command { get; set; }

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
                    OnPropertyChanged(nameof(StatusColor));
                    OnPropertyChanged(nameof(ActionButtonText));
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
            }
        }

        public bool IsRunning => Status == ServiceStatus.Running;
        public string StatusColor => IsRunning ? "#10B981" : "#EF4444";
        public string ActionButtonText => IsRunning ? "Stop" : "Start";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
