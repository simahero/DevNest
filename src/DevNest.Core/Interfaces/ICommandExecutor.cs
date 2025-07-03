using System.Diagnostics;

namespace DevNest.Core.Interfaces
{
    public interface ICommandExecutor
    {
        Task<Process?> StartProcessAsync(string command, string workingDirectory, CancellationToken cancellationToken = default);
        Task<int> ExecuteCommandAsync(string command, string workingDirectory, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
    }
}
