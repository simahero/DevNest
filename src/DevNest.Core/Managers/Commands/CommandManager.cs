using System.Diagnostics;

namespace DevNest.Core.Commands
{
    public class CommandManager
    {
        private readonly LogManager _logManager;

        public CommandManager(LogManager logManager)
        {
            _logManager = logManager;
        }

        public async Task<int> ExecuteCommandAsync(string command, string workingDirectory, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        {
            progress?.Report($"Executing: {command}");

            var processInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c cd /d \"{workingDirectory}\" && {command}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workingDirectory
            };

            progress?.Report("Starting command execution...");

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                _logManager.Log($"Failed to start process for command: {command}");
                throw new InvalidOperationException("Failed to start process");
            }

            // Read output and error streams asynchronously
            var errorTask = ReadStreamAsync(process.StandardError, line =>
            {
                progress?.Report(line);
                _logManager.Log($"[stderr] {line}");
            }, cancellationToken);
            var outputTask = ReadStreamAsync(process.StandardOutput, line => progress?.Report(line), cancellationToken);

            await process.WaitForExitAsync(cancellationToken);
            await Task.WhenAll(outputTask, errorTask);

            progress?.Report($"Command completed with exit code: {process.ExitCode}");
            return process.ExitCode;
        }

        public async Task ExecuteCommandWithSuccessCheckAsync(string command, string workingDirectory, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        {
            var exitCode = await ExecuteCommandAsync(command, workingDirectory, progress, cancellationToken);

            if (exitCode != 0)
            {
                _logManager.Log($"Command failed with exit code {exitCode}: {command}");
                throw new Exception($"Command failed with exit code {exitCode}: {command}");
            }
        }

        public async Task<Process?> StartProcessAsync(string command, string workingDirectory, CancellationToken cancellationToken = default)
        {
            try
            {
                string executable;
                string arguments = string.Empty;
                if (command.StartsWith("\""))
                {
                    var endQuoteIndex = command.IndexOf('"', 1);
                    if (endQuoteIndex > 0)
                    {
                        executable = command.Substring(1, endQuoteIndex - 1);
                        if (command.Length > endQuoteIndex + 1)
                        {
                            arguments = command.Substring(endQuoteIndex + 1).Trim();
                        }
                    }
                    else
                    {
                        // Malformed, fallback
                        _logManager.Log($"Malformed command: {command}");
                        return null;
                    }
                }
                else
                {
                    var commandParts = command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    if (commandParts.Length > 0)
                    {
                        executable = commandParts[0];
                        if (commandParts.Length > 1)
                            arguments = commandParts[1];
                    }
                    else
                    {
                        _logManager.Log($"Malformed command: {command}");
                        return null;
                    }
                }

                var processInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                string existingPath = processInfo.Environment["PATH"] ?? Environment.GetEnvironmentVariable("PATH");
                processInfo.Environment["PATH"] = workingDirectory;

                var process = Process.Start(processInfo);

                var errorTask = ReadStreamAsync(process.StandardError, line =>
                {
                    _ = _logManager.Log($"[stderr] {line}");
                }, cancellationToken);

                var outputTask = ReadStreamAsync(process.StandardOutput, line =>
                {
                    _ = _logManager.Log($"[stdout] {line}");
                }, cancellationToken);

                return await Task.FromResult(process);
            }
            catch (Exception ex)
            {
                _ = _logManager.Log($"Error starting process: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error starting process: {ex.Message}");
                return null;
            }
        }

        private async Task ReadStreamAsync(StreamReader reader, Action<string> onLineRead, CancellationToken cancellationToken = default)
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
            {
                onLineRead(line);
            }
        }
    }
}
