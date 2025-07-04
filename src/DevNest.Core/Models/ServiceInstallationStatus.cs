using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using DevNest.Core.Enums;

namespace DevNest.Core.Models
{
    public partial class ServiceInstallationStatus : ObservableObject
    {
        [ObservableProperty]
        private bool _isInstalling;

        [ObservableProperty]
        private bool _showInstallationPanel;

        [ObservableProperty]
        private string _installationStatus = string.Empty;

        [ObservableProperty]
        private string? _selectedVersion;
    }
}
