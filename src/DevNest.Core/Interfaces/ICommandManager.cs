using System.Diagnostics;
using DevNest.Core.Models;

namespace DevNest.Core.Interfaces
{
    public interface ICommandManager
    {
        Task<(string, string)> GetCommand(ServiceModel service, SettingsModel settings);
    }
}
