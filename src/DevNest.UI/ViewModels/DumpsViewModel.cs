using CommunityToolkit.Mvvm.Input;

namespace DevNest.UI.ViewModels
{
    public partial class DumpsViewModel : BaseViewModel
    {

        public IAsyncRelayCommand LoadDumpsCommand { get; }

        public DumpsViewModel()
        {
            Title = "Database Dumps";
        }


    }
}
