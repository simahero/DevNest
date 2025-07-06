using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using MimeKit;

namespace DevNest.Core.Models
{
    public partial class EmailModel : ObservableObject
    {
        [ObservableProperty]
        private string _subject = string.Empty;
        [ObservableProperty]
        private string _from = string.Empty;
        [ObservableProperty]
        private string _to = string.Empty;
        [ObservableProperty]
        private DateTimeOffset _date;

        [ObservableProperty]
        private string _body = string.Empty;

        [ObservableProperty]
        private string _filePath = string.Empty;

        [ObservableProperty]
        private List<EmailAttachment> _attachments = new();

        public static EmailModel FromMimeMessage(MimeMessage message, string filePath)
        {
            var model = new EmailModel
            {
                Subject = message.Subject ?? string.Empty,
                From = message.From.ToString(),
                To = message.To.ToString(),
                Date = message.Date,
                Body = message.TextBody ?? message.HtmlBody ?? string.Empty,
                FilePath = filePath,
                Attachments = new List<EmailAttachment>()
            };

            foreach (var attachment in message.Attachments)
            {
                if (attachment is MimePart part)
                {
                    var att = new EmailAttachment
                    {
                        FileName = part.FileName,
                        ContentType = part.ContentType.MimeType,
                        Size = part.Content?.Stream?.Length ?? 0
                    };
                    model.Attachments.Add(att);
                }
            }
            return model;
        }
    }
}
