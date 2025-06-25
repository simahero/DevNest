using DevNest.Core.Interfaces;
using System.Diagnostics;

namespace DevNest.Services.Commands
{
    public class CommandExecutionService : ICommandExecutionService
    {
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
                throw new InvalidOperationException("Failed to start process");
            }

            // Read output and error streams asynchronously
            var errorTask = ReadStreamAsync(process.StandardError, line => progress?.Report(line), cancellationToken);
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
                throw new Exception($"Command failed with exit code {exitCode}: {command}");
            }
        }

        public async Task<string> ExecuteCommandWithOutputAsync(string command, string workingDirectory, CancellationToken cancellationToken = default)
        {
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

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start process");
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                throw new Exception($"Command failed with exit code {process.ExitCode}. Error: {error}");
            }

            return output;
        }

        public async Task<Process?> StartProcessAsync(string command, string workingDirectory)
        {
            try
            {
                ProcessStartInfo processInfo;

                // Parse the command to extract executable and arguments
                if (command.Contains("cd /d") && command.Contains("&&"))
                {
                    // Extract the actual command after the cd command
                    var parts = command.Split(new[] { "&&" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        var actualCommand = parts[1].Trim();

                        // If the command is quoted, extract the executable and arguments
                        if (actualCommand.StartsWith("\""))
                        {
                            var endQuoteIndex = actualCommand.IndexOf("\"", 1);
                            if (endQuoteIndex > 0)
                            {
                                var executable = actualCommand.Substring(1, endQuoteIndex - 1);
                                var arguments = actualCommand.Length > endQuoteIndex + 1 ?
                                    actualCommand.Substring(endQuoteIndex + 1).Trim() : "";

                                processInfo = new ProcessStartInfo
                                {
                                    FileName = executable,
                                    Arguments = arguments,
                                    UseShellExecute = false,
                                    CreateNoWindow = false,
                                    WorkingDirectory = workingDirectory
                                };
                            }
                            else
                            {
                                // Fallback to original approach
                                processInfo = CreateFallbackProcessInfo(command, workingDirectory);
                            }
                        }
                        else
                        {
                            // Command without quotes, split by space
                            var commandParts = actualCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            if (commandParts.Length > 0)
                            {
                                processInfo = new ProcessStartInfo
                                {
                                    FileName = commandParts[0],
                                    Arguments = string.Join(" ", commandParts.Skip(1)),
                                    UseShellExecute = false,
                                    CreateNoWindow = false,
                                    WorkingDirectory = workingDirectory
                                };
                            }
                            else
                            {
                                processInfo = CreateFallbackProcessInfo(command, workingDirectory);
                            }
                        }
                    }
                    else
                    {
                        processInfo = CreateFallbackProcessInfo(command, workingDirectory);
                    }
                }
                else
                {
                    // Simple command without cd, parse directly
                    if (command.StartsWith("\""))
                    {
                        var endQuoteIndex = command.IndexOf("\"", 1);
                        if (endQuoteIndex > 0)
                        {
                            var executable = command.Substring(1, endQuoteIndex - 1);
                            var arguments = command.Length > endQuoteIndex + 1 ?
                                command.Substring(endQuoteIndex + 1).Trim() : "";

                            processInfo = new ProcessStartInfo
                            {
                                FileName = executable,
                                Arguments = arguments,
                                UseShellExecute = false,
                                CreateNoWindow = false,
                                WorkingDirectory = workingDirectory
                            };
                        }
                        else
                        {
                            processInfo = CreateFallbackProcessInfo(command, workingDirectory);
                        }
                    }
                    else
                    {
                        var commandParts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (commandParts.Length > 0)
                        {
                            processInfo = new ProcessStartInfo
                            {
                                FileName = commandParts[0],
                                Arguments = string.Join(" ", commandParts.Skip(1)),
                                UseShellExecute = false,
                                CreateNoWindow = false,
                                WorkingDirectory = workingDirectory
                            };
                        }
                        else
                        {
                            processInfo = CreateFallbackProcessInfo(command, workingDirectory);
                        }
                    }
                }

                var process = Process.Start(processInfo);
                return await Task.FromResult(process);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error starting process: {ex.Message}");
                return null;
            }
        }

        private static ProcessStartInfo CreateFallbackProcessInfo(string command, string workingDirectory)
        {
            return new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c cd /d \"{workingDirectory}\" && {command}",
                UseShellExecute = false,
                CreateNoWindow = false,
                WorkingDirectory = workingDirectory
            };
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
