using DevNest.Core.Interfaces;
using System;
using System.IO;

namespace DevNest.Services
{
    public class LogManager
    {
        private readonly IPathService _pathService;
        private readonly string _logFilePath;

        public LogManager(IPathService pathService)
        {
            _pathService = pathService;
            _logFilePath = Path.Combine(_pathService.LogsPath, "debug.log");
            Directory.CreateDirectory(_pathService.LogsPath);
        }

        public void Log(string message)
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
            File.AppendAllText(_logFilePath, logEntry);
        }
    }
}
