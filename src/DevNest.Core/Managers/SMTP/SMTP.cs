using CommunityToolkit.Mvvm.ComponentModel;
using DevNest.Core.Interfaces;
using SmtpServer;
using SmtpServer.Authentication;
using SmtpServer.ComponentModel;


namespace DevNest.Core.Managers.SMTP
{
    public partial class SMTP : ObservableObject
    {

        [ObservableProperty]
        private bool _isRunning;

        [ObservableProperty]
        private int _port = 2525;

        private SmtpServer.SmtpServer _server;
        private readonly IUIDispatcher? _uiDispatcher;

        public SMTP(IUIDispatcher? uiDispatcher = null)
        {
            _uiDispatcher = uiDispatcher;
            var options = new SmtpServerOptionsBuilder()
                .ServerName("DevNest SMTP Server")
                .Port(_port)
                .Build();

            var serviceProvider = new ServiceProvider();
            serviceProvider.Add(new EMLMessageStore());
            serviceProvider.Add((IUserAuthenticator)new DummyUserAuthenticator());

            _server = new SmtpServer.SmtpServer(options, serviceProvider);
        }

        private void SetIsRunning(bool value)
        {
            if (_uiDispatcher != null)
            {
                _uiDispatcher.TryEnqueue(() => IsRunning = value);
            }
            else
            {
                IsRunning = value;
            }
        }

        public async Task StartAsync()
        {
            if (_server != null && !IsRunning)
            {
                System.Diagnostics.Debug.WriteLine($"SMTP server listening on tcp://127.0.0.1:2525");
                SetIsRunning(true);
                await _server.StartAsync(CancellationToken.None);
            }
        }

        public Task StopAsync()
        {
            if (_server != null && IsRunning)
            {
                SetIsRunning(false);
                _server.Shutdown();
                return Task.CompletedTask;
            }
            return Task.CompletedTask;
        }
    }

}
