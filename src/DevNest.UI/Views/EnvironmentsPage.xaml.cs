using DevNest.UI.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevNest.UI.Views;

public sealed partial class EnvironmentsPage : Page
{
    public EnvironmentsViewModel? ViewModel { get; set; }

    public EnvironmentsPage()
    {
        this.InitializeComponent();
    }

    public void SetViewModel(EnvironmentsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
    }
}
