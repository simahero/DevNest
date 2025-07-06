using DevNest.Core.Helpers;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using System.Buffers;

namespace DevNest.Core.Managers.SMTP
{
    public class EMLMessageStore : MessageStore
    {
        public override async Task<SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            await using var stream = new MemoryStream();

            var position = buffer.GetPosition(0);
            while (buffer.TryGet(ref position, out var memory))
            {
                await stream.WriteAsync(memory, cancellationToken);
            }

            stream.Position = 0;

            var message = await MimeKit.MimeMessage.LoadAsync(stream, cancellationToken);
            var mailDir = Path.Combine(PathHelper.LogsPath, "smtp");
            var mailPath = Path.Combine(mailDir, $"{Guid.NewGuid()}.eml");

            if (!await FileSystemHelper.DirectoryExistsAsync(mailDir))
            {
                await FileSystemHelper.CreateDirectoryAsync(mailDir);
            }

            await using var fileStream = File.Create(mailPath);
            await message.WriteToAsync(fileStream, cancellationToken);

            return SmtpResponse.Ok;
        }
    }
}
