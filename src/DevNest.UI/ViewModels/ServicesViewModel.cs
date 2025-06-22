using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevNest.Core.Interfaces;
using DevNest.Core.Models;

namespace DevNest.UI.ViewModels
{
    public partial class ServicesViewModel : BaseViewModel
    {
        private readonly IServiceManager _serviceManager;

        public ObservableCollection<Service> Services { get; } = new();

        public ServicesViewModel(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
            Title = "Services";
            LoadServicesCommand = new AsyncRelayCommand(LoadServicesAsync);
            StartServiceCommand = new AsyncRelayCommand<string>(StartServiceAsync);
            StopServiceCommand = new AsyncRelayCommand<string>(StopServiceAsync);
        }

        public IAsyncRelayCommand LoadServicesCommand { get; }
        public IAsyncRelayCommand<string> StartServiceCommand { get; }
        public IAsyncRelayCommand<string> StopServiceCommand { get; }

        private async Task LoadServicesAsync()
        {
            IsLoading = true;
            try
            {
                var services = await _serviceManager.GetServicesAsync();
                Services.Clear();
                foreach (var service in services)
                {
                    Services.Add(service);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task StartServiceAsync(string? serviceName)
        {
            if (string.IsNullOrEmpty(serviceName)) return;
            
            try
            {
                await _serviceManager.StartServiceAsync(serviceName);
                await LoadServicesAsync();
            }
            catch (Exception ex)
            {
                // Handle error
                System.Diagnostics.Debug.WriteLine($"Error starting service: {ex.Message}");
            }
        }

        private async Task StopServiceAsync(string? serviceName)
        {
            if (string.IsNullOrEmpty(serviceName)) return;
            
            try
            {
                await _serviceManager.StopServiceAsync(serviceName);
                await LoadServicesAsync();
            }
            catch (Exception ex)
            {
                // Handle error
                System.Diagnostics.Debug.WriteLine($"Error stopping service: {ex.Message}");
            }
        }

        protected override async Task OnLoadedAsync()
        {
            await LoadServicesCommand.ExecuteAsync(null);
        }
    }
}
