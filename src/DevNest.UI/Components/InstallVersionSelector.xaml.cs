using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Windows.Input;
using DevNest.Core.Models;

namespace DevNest.UI.Components
{
    public sealed partial class InstallVersionSelector : UserControl
    {
        public InstallVersionSelector()
        {
            this.InitializeComponent();
        }

        public static readonly DependencyProperty InstallableVersionsProperty =
            DependencyProperty.Register(
                nameof(InstallableVersions),
                typeof(ObservableCollection<ServiceDefinition>),
                typeof(InstallVersionSelector),
                new PropertyMetadata(null));

        public ObservableCollection<ServiceDefinition> InstallableVersions
        {
            get => (ObservableCollection<ServiceDefinition>)GetValue(InstallableVersionsProperty);
            set => SetValue(InstallableVersionsProperty, value);
        }

        public static readonly DependencyProperty SelectedVersionProperty =
            DependencyProperty.Register(
                nameof(SelectedVersion),
                typeof(string),
                typeof(InstallVersionSelector),
                new PropertyMetadata(null));

        public string SelectedVersion
        {
            get => (string)GetValue(SelectedVersionProperty);
            set => SetValue(SelectedVersionProperty, value);
        }

        public static readonly DependencyProperty InstallCommandProperty =
            DependencyProperty.Register(
                nameof(InstallCommand),
                typeof(ICommand),
                typeof(InstallVersionSelector),
                new PropertyMetadata(null));

        public ICommand InstallCommand
        {
            get => (ICommand)GetValue(InstallCommandProperty);
            set => SetValue(InstallCommandProperty, value);
        }

        public static readonly DependencyProperty InstallCommandParameterProperty =
            DependencyProperty.Register(
                nameof(InstallCommandParameter),
                typeof(object),
                typeof(InstallVersionSelector),
                new PropertyMetadata(null));

        public object InstallCommandParameter
        {
            get => GetValue(InstallCommandParameterProperty);
            set => SetValue(InstallCommandParameterProperty, value);
        }

        public static readonly DependencyProperty ShowInstallationPanelProperty =
            DependencyProperty.Register(
                nameof(ShowInstallationPanel),
                typeof(bool),
                typeof(InstallVersionSelector),
                new PropertyMetadata(false));

        public bool ShowInstallationPanel
        {
            get => (bool)GetValue(ShowInstallationPanelProperty);
            set => SetValue(ShowInstallationPanelProperty, value);
        }

        public static readonly DependencyProperty InstallationStatusProperty =
            DependencyProperty.Register(
                nameof(InstallationStatus),
                typeof(string),
                typeof(InstallVersionSelector),
                new PropertyMetadata(string.Empty));

        public string InstallationStatus
        {
            get => (string)GetValue(InstallationStatusProperty);
            set => SetValue(InstallationStatusProperty, value);
        }

        public static readonly DependencyProperty IsInstallingProperty =
            DependencyProperty.Register(
                nameof(IsInstalling),
                typeof(bool),
                typeof(InstallVersionSelector),
                new PropertyMetadata(false));

        public bool IsInstalling
        {
            get => (bool)GetValue(IsInstallingProperty);
            set => SetValue(IsInstallingProperty, value);
        }
    }
}
