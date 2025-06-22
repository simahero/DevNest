using DevNest.Core.Models;
using System.Threading.Tasks;

namespace DevNest.Core.Interfaces
{
    public interface IAppSettingsService
    {
        Task<AppSettings> LoadSettingsAsync();
        Task SaveSettingsAsync(AppSettings settings);
        Task<AppSettings> GetDefaultSettingsAsync();
        Task ResetSettingsAsync();
    }
}
