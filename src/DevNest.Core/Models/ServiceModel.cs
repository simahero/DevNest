using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using DevNest.Core.Enums;

namespace DevNest.Core.Models
{

    public partial class ServiceModel : ObservableObject
    {
        [ObservableProperty]
        private ServiceStatus _status;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private Process? _process;

        [ObservableProperty]
        private bool _isSelected = false;

        [ObservableProperty]
        private string _path = string.Empty;

        public required string Name { get; set; }
        public required string DisplayName { get; set; }
        public required string Command { get; set; }
        public ServiceType ServiceType { get; set; }

        public string ProcessId => Process?.Id.ToString() ?? "N/A";

        public bool IsRunning => Status == ServiceStatus.Running;

        public string? WorkingDirectory { get; set; }

        partial void OnStatusChanged(ServiceStatus value)
        {
            OnPropertyChanged(nameof(IsRunning));
        }

        partial void OnProcessChanged(Process? value)
        {
            OnPropertyChanged(nameof(ProcessId));
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
