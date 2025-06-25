using System.Diagnostics;

namespace DevNest.Core.Interfaces
{
    public interface ICommandExecutionService
    {
        Task<int> ExecuteCommandAsync(string command, string workingDirectory, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
        Task ExecuteCommandWithSuccessCheckAsync(string command, string workingDirectory, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
        Task<string> ExecuteCommandWithOutputAsync(string command, string workingDirectory, CancellationToken cancellationToken = default);
        Task<Process?> StartProcessAsync(string command, string workingDirectory);
    }
}
