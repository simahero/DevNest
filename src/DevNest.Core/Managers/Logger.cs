using DevNest.Core.Helpers;

namespace DevNest.Core
{
    public class Logger
    {

        public Logger()
        {
            Task.Run(async () => await FileSystemHelper.CreateDirectoryAsync(PathHelper.LogsPath)).Wait();
        }

        public static async Task Log(string message)
        {
            var _logFilePath = Path.Combine(PathHelper.LogsPath, "debug.log");
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
            await FileSystemHelper.AppendAllTextAsync(_logFilePath, logEntry);
        }
    }
}
