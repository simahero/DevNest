using DevNest.Core.Events;

namespace DevNest.Core.Interfaces
{
    /// <summary>
    /// Event bus for application-wide events
    /// </summary>
    public interface IEventBus
    {
        event EventHandler<AppStateChangedEventArgs> AppStateChanged;
        void PublishAppStateChanged(AppStateChangeType changeType, object? data = null);
    }
}
