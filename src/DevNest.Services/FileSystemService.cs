using DevNest.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DevNest.Services
{
    public class FileSystemService : IFileSystemService
    {
        public Task<bool> DirectoryExistsAsync(string path)
        {
            return Task.FromResult(Directory.Exists(path));
        }

        public Task<bool> FileExistsAsync(string path)
        {
            return Task.FromResult(File.Exists(path));
        }

        public Task CreateDirectoryAsync(string path)
        {
            Directory.CreateDirectory(path);
            return Task.CompletedTask;
        }

        public Task DeleteDirectoryAsync(string path, bool recursive = false)
        {
            Directory.Delete(path, recursive);
            return Task.CompletedTask;
        }

        public async Task<string> ReadAllTextAsync(string filePath)
        {
            return await File.ReadAllTextAsync(filePath);
        }

        public async Task WriteAllTextAsync(string filePath, string content)
        {
            await File.WriteAllTextAsync(filePath, content);
        }

        public Task<IEnumerable<string>> GetFilesAsync(string directory, string searchPattern = "*")
        {
            var files = Directory.GetFiles(directory, searchPattern);
            return Task.FromResult<IEnumerable<string>>(files);
        }

        public Task<IEnumerable<string>> GetDirectoriesAsync(string directory)
        {
            var directories = Directory.GetDirectories(directory);
            return Task.FromResult<IEnumerable<string>>(directories);
        }

        public async Task<FileInfo> GetFileInfoAsync(string filePath)
        {
            return await Task.FromResult(new FileInfo(filePath));
        }

        public async Task CopyFileAsync(string sourceFile, string destinationFile)
        {
            await Task.Run(() => File.Copy(sourceFile, destinationFile));
        }

        public async Task MoveFileAsync(string sourceFile, string destinationFile)
        {
            await Task.Run(() => File.Move(sourceFile, destinationFile));
        }

        public Task DeleteFileAsync(string filePath)
        {
            File.Delete(filePath);
            return Task.CompletedTask;
        }
    }
}
