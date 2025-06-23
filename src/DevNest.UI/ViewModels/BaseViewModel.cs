using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DevNest.UI.ViewModels
{
    public abstract class BaseViewModel : ObservableObject
    {
        private bool _isLoading;
        private string _title = string.Empty;

        public BaseViewModel()
        {
            LoadCommand = new AsyncRelayCommand(LoadAsync);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public IAsyncRelayCommand LoadCommand { get; }

        protected virtual Task OnLoadedAsync() => Task.CompletedTask;
        protected virtual Task OnUnloadedAsync() => Task.CompletedTask;

        public virtual async Task LoadAsync()
        {
            await OnLoadedAsync();
        }
    }
}
