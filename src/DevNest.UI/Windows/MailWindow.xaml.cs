using DevNest.UI.ViewModels;
using Microsoft.UI.Xaml;

namespace DevNest.UI.Windows
{
    public sealed partial class MailWindow : Window
    {
        public MailWindowViewModel ViewModel { get; } = new MailWindowViewModel();
        public MailWindowViewModel DataContext { get; set; }

        public MailWindow()
        {
            this.InitializeComponent();

            ViewModel = new MailWindowViewModel();
            this.DataContext = ViewModel;

            ViewModel.CloseRequested += (s, e) => this.Close();

            ViewModel.ShowEmailBodyRequested += bodyHtml =>
            {
                if (MailWebView != null)
                {
                    MailWebView.NavigateToString(bodyHtml ?? "<html><body><p>No content.</p></body></html>");
                }
            };

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
