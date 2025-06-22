using DevNest.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DevNest.Core.Interfaces
{
    public interface IDumpService
    {
        Task<IEnumerable<DumpFile>> GetDumpFilesAsync();
        Task<DumpFile> CreateDumpAsync(string databaseName, string outputPath);
        Task ImportDumpAsync(string dumpFilePath, string targetDatabase);
        Task DeleteDumpAsync(string dumpFilePath);
        Task<bool> ValidateDumpFileAsync(string filePath);
    }
}
