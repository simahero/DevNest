using DevNest.UI.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevNest.UI.Views;

public sealed partial class DashboardPage : Page
{

    public DashboardViewModel? ViewModel { get; set; }

    public DashboardPage()
    {
        this.InitializeComponent();
    }

    public void SetViewModel(DashboardViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
    }
}