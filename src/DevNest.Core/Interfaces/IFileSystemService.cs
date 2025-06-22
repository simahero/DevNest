using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DevNest.Core.Interfaces
{
    public interface IFileSystemService
    {
        Task<bool> DirectoryExistsAsync(string path);
        Task<bool> FileExistsAsync(string path);
        Task CreateDirectoryAsync(string path);
        Task DeleteDirectoryAsync(string path, bool recursive = false);
        Task<string> ReadAllTextAsync(string filePath);
        Task WriteAllTextAsync(string filePath, string content);
        Task<IEnumerable<string>> GetFilesAsync(string directory, string searchPattern = "*");
        Task<IEnumerable<string>> GetDirectoriesAsync(string directory);
        Task<FileInfo> GetFileInfoAsync(string filePath);
        Task CopyFileAsync(string sourceFile, string destinationFile);
        Task MoveFileAsync(string sourceFile, string destinationFile);
        Task DeleteFileAsync(string filePath);
    }
}
