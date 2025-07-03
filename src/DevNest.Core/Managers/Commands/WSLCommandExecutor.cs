using DevNest.Core.Interfaces;
using System.Diagnostics;

namespace DevNest.Core.Managers.Commands
{
    public class WSLCommandExecutor : ICommandExecutor
    {
        public Task<Process?> StartProcessAsync(string command, string workingDirectory, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<int> ExecuteCommandAsync(string command, string workingDirectory, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
