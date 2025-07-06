using DevNest.UI.ViewModels;
using Microsoft.UI.Xaml;

namespace DevNest.UI.Windows
{
    public sealed partial class PHPExtensionWindow : Window
    {
        public PHPExtensionWindowViewModel ViewModel { get; }
        public PHPExtensionWindowViewModel DataContext { get; set; }

        public PHPExtensionWindow(string phpIniPath)
        {

            ViewModel = new PHPExtensionWindowViewModel(phpIniPath);
            this.DataContext = ViewModel;

            this.InitializeComponent();

            ViewModel.CloseRequested += (s, e) => this.Close();

            try
            {
                this.ExtendsContentIntoTitleBar = true;
            }
            catch
            {
            }

            _ = ViewModel.LoadCommand.ExecuteAsync(null);
        }
    }
}
