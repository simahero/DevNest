using DevNest.Core.Models;

namespace DevNest.Core.Events
{
    /// <summary>
    /// Event arguments for application state changes
    /// </summary>
    public class AppStateChangedEventArgs : EventArgs
    {
        public AppStateChangeType ChangeType { get; }
        public object? ChangedData { get; }

        public AppStateChangedEventArgs(AppStateChangeType changeType, object? changedData = null)
        {
            ChangeType = changeType;
            ChangedData = changedData;
        }
    }

    public enum AppStateChangeType
    {
        SettingsChanged,
        SitesChanged,
        ServicesChanged,
        AvailableSitesChanged,
        AvailableServicesChanged,
        ServiceStatusChanged
    }
}
