using DevNest.Core.Helpers;
using DevNest.Core.Models;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DevNest.UI.ViewModels
{
    public partial class MailWindowViewModel : BaseViewModel
    {

        private EmailModel? _selectedEmail;
        public EmailModel? SelectedEmail
        {
            get => _selectedEmail;
            set
            {
                if (SetProperty(ref _selectedEmail, value))
                {
                    OnSelectedEmailChanged();
                }
            }
        }

        public ObservableCollection<EmailModel> Emails { get; } = new();

        public event Action<string?>? ShowEmailBodyRequested;
        public event EventHandler? CloseRequested;

        public MailWindowViewModel()
        {
            Title = "Mails";
        }


        protected override async Task OnLoadedAsync()
        {
            await LoadEmailsAsync();
        }

        private async Task LoadEmailsAsync()
        {
            Emails.Clear();
            var mailsDir = Path.Combine(PathHelper.LogsPath, "smtp");
            if (!Directory.Exists(mailsDir))
            {
                return;
            }

            var files = Directory.GetFiles(mailsDir, "*.eml");
            var emailList = new List<EmailModel>();
            foreach (var file in files)
            {
                try
                {
                    using var stream = File.OpenRead(file);
                    var message = await Task.Run(() => MimeMessage.Load(stream));
                    var model = EmailModel.FromMimeMessage(message, file);
                    emailList.Add(model);
                }
                catch { /* Optionally log or handle errors */ }
            }
            foreach (var model in emailList.OrderByDescending(e => e.Date))
            {
                Emails.Add(model);
            }
            // if (Emails.Count > 0)
            // {
            //     SelectedEmail = Emails[0];
            // }
        }

        private void OnSelectedEmailChanged()
        {
            if (SelectedEmail != null)
            {
                ShowEmailBodyRequested?.Invoke(SelectedEmail.Body);
            }
            else
            {
                ShowEmailBodyRequested?.Invoke(null);
            }
        }

    }
}
