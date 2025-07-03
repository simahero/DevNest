namespace DevNest.Core.Helpers
{
    public static class FileSystemHelper
    {
        public static Task<bool> DirectoryExistsAsync(string path)
        {
            return Task.Run(() => Directory.Exists(path));
        }

        public static Task<bool> FileExistsAsync(string path)
        {
            return Task.Run(() => File.Exists(path));
        }

        public static Task CreateDirectoryAsync(string path)
        {
            return Task.Run(() => Directory.CreateDirectory(path));
        }

        public static async Task<IEnumerable<string>> GetFilesAsync(string directory, string searchPattern = "*")
        {
            return (await Task.Run(() => Directory.GetFiles(directory, searchPattern))).AsEnumerable();
        }

        public static async Task<IEnumerable<string>> GetDirectoriesAsync(string directory)
        {
            return (await Task.Run(() => Directory.GetDirectories(directory))).AsEnumerable();
        }

        public static Task<FileInfo> GetFileInfoAsync(string filePath)
        {
            return Task.Run(() => new FileInfo(filePath));
        }

        public static async Task CopyDirectory(string sourceDir, string destDir, bool overwrite)
        {
            await Task.Run(() =>
            {

                if (overwrite && Directory.Exists(destDir))
                {
                    Directory.Delete(destDir, true);
                }

                Directory.CreateDirectory(destDir);
                foreach (var dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(sourceDir, dirPath);
                    var destPath = Path.Combine(destDir, relativePath);
                    Directory.CreateDirectory(destPath);
                }

                foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(sourceDir, file);
                    var destFile = Path.Combine(destDir, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                    if (!File.Exists(destFile) || overwrite)
                    {
                        File.Copy(file, destFile, overwrite);
                    }
                }
            });
        }

        public static async Task CopyFileAsync(string sourceFile, string destinationFile)
        {
            await Task.Run(() => File.Copy(sourceFile, destinationFile));
        }

        public static async Task MoveFileAsync(string sourceFile, string destinationFile)
        {
            await Task.Run(() => File.Move(sourceFile, destinationFile));
        }

        public static Task<string> ReadAllTextAsync(string path)
        {
            return Task.Run(() => File.ReadAllText(path));
        }

        public static Task DeleteDirectoryAsync(string path, bool recursive)
        {
            return Task.Run(() => Directory.Delete(path, recursive));
        }

        public static Task DeleteFileAsync(string path)
        {
            return Task.Run(() => File.Delete(path));
        }

        public static Task<DateTime> GetDirectoryCreationTimeAsync(string path)
        {
            return Task.Run(() => Directory.GetCreationTime(path));
        }

        public static async Task AppendAllTextAsync(string path, string contents)
        {
            await Task.Run(() => File.AppendAllText(path, contents));
        }

        public static async Task WriteAllTextAsync(string path, string contents)
        {
            await WriteFileWithRetryAsync(path, contents);
        }

        public static async Task WriteAllBytesAsync(string path, byte[] bytes)
        {
            await Task.Run(() => File.WriteAllBytes(path, bytes));
        }

        public static async Task<string> ReadFileWithRetryAsync(string filePath, int maxRetries = 3)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(stream);
                    return await reader.ReadToEndAsync();
                }
                catch (IOException) when (i < maxRetries - 1)
                {
                    await Task.Delay(100 * (i + 1)); // Progressive delay
                }
            }
            throw new IOException($"Could not read file {filePath} after {maxRetries} attempts");
        }

        public static async Task WriteFileWithRetryAsync(string filePath, string content, int maxRetries = 3)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                    using var writer = new StreamWriter(stream);
                    await writer.WriteAsync(content);
                    return;
                }
                catch (IOException) when (i < maxRetries - 1)
                {
                    await Task.Delay(100 * (i + 1)); // Progressive delay
                }
            }
            throw new IOException($"Could not write file {filePath} after {maxRetries} attempts");
        }

    }
}
