using DevNest.UI.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevNest.UI.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel? ViewModel { get; set; }

    public SettingsPage()
    {
        this.InitializeComponent();
    }

    public void SetViewModel(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
    }
}
