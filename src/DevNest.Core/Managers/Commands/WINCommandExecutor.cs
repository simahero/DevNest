using DevNest.Core.Interfaces;
using System.Diagnostics;

namespace DevNest.Core.Managers.Commands
{
    public class WINCommandExecutor : ICommandExecutor
    {
        public async Task<Process?> StartProcessAsync(string command, string workingDirectory, CancellationToken cancellationToken = default)
        {
            try
            {
                string executable;
                string arguments = string.Empty;

                var commandParts = command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

                if (commandParts.Length > 0)
                {
                    executable = commandParts[0];
                    if (commandParts.Length > 1)
                    {
                        arguments = commandParts[1];
                    }
                }
                else
                {
                    _ = Logger.Log($"Malformed command: {command}");
                    return null;
                }

                var executablePath = Path.Combine(workingDirectory, executable);
                if (!File.Exists(executablePath))
                {
                    _ = Logger.Log($"Executable not found at: {executablePath}");
                    return null;
                }

                _ = Logger.Log($"executable: {executable}");
                _ = Logger.Log($"arguments: {arguments}");

                var processInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                string existingPath = processInfo.Environment["PATH"] ?? Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                processInfo.Environment["PATH"] = $"{workingDirectory};{existingPath}";

                var process = Process.Start(processInfo);

                if (process == null)
                {
                    _ = Logger.Log($"Failed to start process for command: {command}");
                    return null;
                }

                var errorTask = Task.Run(async () =>
                {
                    string? line;
                    while ((line = await process.StandardError.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
                    {
                        _ = Logger.Log($"[stderr] {line}");
                    }
                }, cancellationToken);

                var outputTask = Task.Run(async () =>
                {
                    string? line;
                    while ((line = await process.StandardOutput.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
                    {
                        _ = Logger.Log($"[stdout] {line}");
                    }
                }, cancellationToken);

                return await Task.FromResult(process);
            }
            catch (Exception ex)
            {
                _ = Logger.Log($"Error starting process: {ex.Message}");
                return null;
            }
        }

        public async Task<int> ExecuteCommandAsync(string command, string workingDirectory, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        {
            progress?.Report($"Executing: {command}");

            var processInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"{command}",
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
                _ = Logger.Log($"Failed to start process for command: {command}");
                progress?.Report($"Failed to start process for command: {command}");
                throw new InvalidOperationException("Failed to start process");
            }

            var errorTask = Task.Run(async () =>
            {
                string? line;
                while ((line = await process.StandardError.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
                {
                    _ = Logger.Log($"[stderr] {line}");
                }
            }, cancellationToken);

            var outputTask = Task.Run(async () =>
            {
                string? line;
                while ((line = await process.StandardOutput.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
                {
                    _ = Logger.Log($"[stdout] {line}");
                }
            }, cancellationToken);

            await process.WaitForExitAsync(cancellationToken);
            await Task.WhenAll(outputTask, errorTask);

            progress?.Report($"Command completed with exit code: {process.ExitCode}");
            return process.ExitCode;
        }

    }
}
