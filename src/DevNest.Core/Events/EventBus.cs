using DevNest.Core.Interfaces;

namespace DevNest.Core.Events
{
    public class EventBus : IEventBus
    {
        public event EventHandler<AppStateChangedEventArgs>? AppStateChanged;

        public void PublishAppStateChanged(AppStateChangeType changeType, object? data = null)
        {
            AppStateChanged?.Invoke(this, new AppStateChangedEventArgs(changeType, data));
        }
    }
}
