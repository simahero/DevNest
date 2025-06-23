using DevNest.UI.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace DevNest.UI.Views;

public sealed partial class ServicesPage : Page
{
    public ServicesViewModel? ViewModel { get; set; }

    public ServicesPage()
    {
        this.InitializeComponent();
    }

    public void SetViewModel(ServicesViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
    }
}
