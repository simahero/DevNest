using DevNest.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevNest.UI.Views;

public sealed partial class SitesPage : Page
{
    public SitesViewModel? ViewModel { get; set; }

    public SitesPage()
    {
        this.InitializeComponent();
    }

    public void SetViewModel(SitesViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
    }
}