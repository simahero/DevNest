using DevNest.Core.Files;

namespace DevNest.Core
{
    public class LogManager
    {
        private readonly string _logFilePath;

        public LogManager()
        {
            _logFilePath = Path.Combine(PathManager.LogsPath, "debug.log");
            Task.Run(async () => await FileSystemManager.CreateDirectoryAsync(PathManager.LogsPath)).Wait();
        }

        public async Task Log(string message)
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
            await FileSystemManager.AppendAllTextAsync(_logFilePath, logEntry);
        }
    }
}
