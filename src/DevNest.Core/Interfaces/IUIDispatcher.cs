namespace DevNest.Core.Interfaces
{
    public interface IUIDispatcher
    {
        bool TryEnqueue(Action callback);
    }
}