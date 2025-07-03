using Microsoft.UI.Xaml;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevNest.UI.ViewModels
{
    public partial class PhpExtensionViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _enabled;

        public string Name { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public string OriginalLine { get; set; } = string.Empty;

        public bool HasComment => !string.IsNullOrEmpty(Comment);
        public Visibility CommentVisibility => HasComment ? Visibility.Visible : Visibility.Collapsed;
    }
}