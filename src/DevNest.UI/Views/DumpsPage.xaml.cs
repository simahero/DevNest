using Microsoft.UI.Xaml.Controls;
using DevNest.UI.ViewModels;

namespace DevNest.UI.Views;

public sealed partial class DumpsPage : Page
{
    public DumpsViewModel? ViewModel { get; set; }

    public DumpsPage()
    {
        this.InitializeComponent();
    }

    public void SetViewModel(DumpsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
    }
}