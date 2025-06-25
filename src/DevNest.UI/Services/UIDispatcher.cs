using DevNest.Core.Interfaces;
using Microsoft.UI.Dispatching;
using System;

namespace DevNest.UI.Services
{
    public class UIDispatcher : IUIDispatcher
    {
        private readonly DispatcherQueue _dispatcherQueue;

        public UIDispatcher(DispatcherQueue dispatcherQueue)
        {
            _dispatcherQueue = dispatcherQueue;
        }

        public bool TryEnqueue(Action callback)
        {
            return _dispatcherQueue.TryEnqueue(() => callback());
        }
    }
}