using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace DevNest.UI.ViewModels
{
    public partial class DumpsViewModel : BaseViewModel
    {

        public DumpsViewModel()
        {
            Title = "Database Dumps";
        }

        [RelayCommand]
        private Task LoadDumpsAsync()
        {
            // TODO: Implement loading dumps logic
            return Task.CompletedTask;
        }
    }
}
