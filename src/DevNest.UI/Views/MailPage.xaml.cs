using DevNest.UI.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevNest.UI.Views;

public sealed partial class MailPage : Page
{
    public MailViewModel? ViewModel { get; set; }

    public MailPage()
    {
        this.InitializeComponent();
    }

    public void SetViewModel(MailViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
    }
}
