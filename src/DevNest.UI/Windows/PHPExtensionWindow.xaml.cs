using DevNest.UI.ViewModels;
using Microsoft.UI.Xaml;

namespace DevNest.UI.Windows
{
    public sealed partial class PHPExtensionWindow : Window
    {
        public PHPExtensionWindowViewModel? ViewModel { get; set; }
        public PHPExtensionWindowViewModel DataContext { get; }

        public PHPExtensionWindow(string phpIniPath)
        {
            this.InitializeComponent();

            ViewModel = new PHPExtensionWindowViewModel(phpIniPath);
            this.DataContext = ViewModel;

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
