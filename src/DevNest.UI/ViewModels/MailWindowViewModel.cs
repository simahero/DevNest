using DevNest.Core.Helpers;
using DevNest.Core.Models;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

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

        public IRelayCommand<EmailAttachment> OpenAttachmentCommand { get; }

        public MailWindowViewModel()
        {
            Title = "Mails";
            OpenAttachmentCommand = new RelayCommand<EmailAttachment>(OpenAttachment);
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

        private static string SanitizeFileName(string fileName)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName;
        }

        private async void OpenAttachment(EmailAttachment? attachment)
        {
            if (attachment == null || SelectedEmail == null)
            {
                return;
            }

            var emailFilePath = SelectedEmail.FilePath;
            if (string.IsNullOrEmpty(emailFilePath) || !System.IO.File.Exists(emailFilePath))
            {
                return;
            }

            try
            {
                using var stream = File.OpenRead(emailFilePath);
                var message = await Task.Run(() => MimeMessage.Load(stream));
                var mimePart = message.Attachments
                    .OfType<MimePart>()
                    .FirstOrDefault(p => p.FileName == attachment.FileName);
                if (mimePart == null)
                    return;

                var safeFileName = SanitizeFileName(attachment.FileName);
                var tempPath = Path.Combine(Path.GetTempPath(), safeFileName);
                using (var fileStream = File.Create(tempPath))
                {
                    mimePart.Content.DecodeTo(fileStream);
                }

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true
                };

                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to open attachment: {ex.Message}");
            }
        }

    }
}
