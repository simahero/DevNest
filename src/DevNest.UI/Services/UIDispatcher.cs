using DevNest.Core.Interfaces;
using Microsoft.UI.Dispatching;
using System;

namespace DevNest.UI.Services
{
    public class UIDispatcher : IUIDispatcher
    {
        private readonly DispatcherQueue _dispatcherQueue;

        public UIDispatcher()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        }

        public bool TryEnqueue(Action callback)
        {
            return _dispatcherQueue.TryEnqueue(() => callback());
        }
    }
}