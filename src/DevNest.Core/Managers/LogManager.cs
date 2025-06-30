using DevNest.Core.Files;

namespace DevNest.Core
{
    public class LogManager
    {
        private readonly PathManager _pathManager;
        private readonly string _logFilePath;

        public LogManager(PathManager pathManager)
        {
            _pathManager = pathManager;
            _logFilePath = Path.Combine(_pathManager.LogsPath, "debug.log");
            Directory.CreateDirectory(_pathManager.LogsPath);
        }

        public void Log(string message)
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
            File.AppendAllText(_logFilePath, logEntry);
        }
    }
}
