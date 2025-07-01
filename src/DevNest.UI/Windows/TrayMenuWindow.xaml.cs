using Microsoft.UI.Xaml;

namespace DevNest.UI.Windows
{
    public sealed partial class TrayMenuWindow : Window
    {
        public TrayMenuWindow()
        {
            this.InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(TrayTitleBar);

            this.Activated += Window_Activated;
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
                this.Close();
            }
        }
    }
}
