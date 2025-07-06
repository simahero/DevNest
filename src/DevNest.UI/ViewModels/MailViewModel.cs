using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevNest.Core.Managers.SMTP;
using DevNest.UI.Windows;
using System;
using System.Threading.Tasks;

namespace DevNest.UI.ViewModels
{
    public partial class MailViewModel : BaseViewModel
    {
        private readonly SMTP _smtpServer;

        [ObservableProperty]
        private bool _isLoading;

        public MailViewModel(SMTP smtpServer)
        {
            _smtpServer = smtpServer;
            Title = "Mail";
            IsLoading = false;
        }

        public SMTP SmtpServer => _smtpServer;

        [RelayCommand]
        private async Task ToggleSMTP()
        {
            IsLoading = true;
            try
            {
                if (!_smtpServer.IsRunning)
                {
                    await _smtpServer.StartAsync();
                }
                else
                {
                    await _smtpServer.StopAsync();
                }
            }
            catch (Exception)
            {
                // Handle exception (e.g., log it or show a message to the user)
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void OpenMails()
        {
            var mailWindow = new MailWindow();
            mailWindow.Activate();
        }

    }
}
