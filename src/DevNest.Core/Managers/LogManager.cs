using DevNest.Core.Helpers;

namespace DevNest.Core
{
    public class LogManager
    {

        public LogManager()
        {

            Task.Run(async () => await FileSystemManager.CreateDirectoryAsync(PathManager.LogsPath)).Wait();
        }

        public static async Task Log(string message)
        {
            var _logFilePath = Path.Combine(PathManager.LogsPath, "debug.log");
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
            await FileSystemManager.AppendAllTextAsync(_logFilePath, logEntry);
        }
    }
}
