using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevNest.Core.Models
{
    public partial class SiteModel : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _path = string.Empty;

        [ObservableProperty]
        private string _url = string.Empty;

        [ObservableProperty]
        private bool _isInstalled;

        [ObservableProperty]
        private bool _isActive;

        [ObservableProperty]
        private Process? _shareProcess;

        public bool IsSharing => ShareProcess != null && !ShareProcess.HasExited;

        partial void OnShareProcessChanged(Process? value)
        {
            OnPropertyChanged(nameof(IsSharing));
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
