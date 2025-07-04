using CommunityToolkit.Mvvm.Input;
using DevNest.Core.State;
using System;

namespace DevNest.UI.ViewModels
{
    public partial class SettingsViewModel : BaseViewModel
    {
        private readonly AppState _appState;

        public SettingsViewModel(AppState appState)
        {
            _appState = appState;
            Title = "Settings";
        }

        public AppState AppState => _appState;

        [RelayCommand]
        private void BrowseFolder()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Browse folder functionality needs to be implemented");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error browsing folder: {ex.Message}");
            }
        }
    }
}
