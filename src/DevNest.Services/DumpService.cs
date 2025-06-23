using DevNest.Core.Interfaces;
using DevNest.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DevNest.Services
{
    public class DumpService : IDumpService
    {
        private readonly IFileSystemService _fileSystemService;
        private readonly IAppSettingsService _appSettingsService;

        public DumpService(IFileSystemService fileSystemService, IAppSettingsService appSettingsService)
        {
            _fileSystemService = fileSystemService;
            _appSettingsService = appSettingsService;
        }

        public async Task<IEnumerable<DumpFile>> GetDumpFilesAsync()
        {
            var settings = await _appSettingsService.LoadSettingsAsync();
            var dumpsPath = Path.Combine(settings.InstallDirectory, "dumps");

            if (!await _fileSystemService.DirectoryExistsAsync(dumpsPath))
            {
                return new List<DumpFile>();
            }

            var dumpFiles = new List<DumpFile>();
            var files = await _fileSystemService.GetFilesAsync(dumpsPath, "*.sql");

            foreach (var filePath in files)
            {
                try
                {
                    var fileInfo = await _fileSystemService.GetFileInfoAsync(filePath);
                    var dumpFile = new DumpFile
                    {
                        Name = Path.GetFileName(filePath),
                        Path = filePath,
                        Size = fileInfo.Length,
                        CreatedDate = fileInfo.CreationTime
                    };

                    dumpFiles.Add(dumpFile);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error reading dump file {filePath}: {ex.Message}");
                }
            }

            return dumpFiles.OrderByDescending(d => d.CreatedDate);
        }

        public async Task<DumpFile> CreateDumpAsync(string databaseName, string outputPath)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database name cannot be empty.", nameof(databaseName));

            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Output path cannot be empty.", nameof(outputPath));

            try
            {
                // Ensure the output directory exists
                var outputDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDir) && !await _fileSystemService.DirectoryExistsAsync(outputDir))
                {
                    await _fileSystemService.CreateDirectoryAsync(outputDir);
                }

                // TODO: Implement actual database dump logic based on database type
                // For now, create a placeholder file
                var dumpContent = $"-- Database dump for: {databaseName}\n-- Created: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n-- TODO: Implement actual dump logic\n";
                await _fileSystemService.WriteAllTextAsync(outputPath, dumpContent);

                var fileInfo = await _fileSystemService.GetFileInfoAsync(outputPath);
                return new DumpFile
                {
                    Name = Path.GetFileName(outputPath),
                    Path = outputPath,
                    Size = fileInfo.Length,
                    CreatedDate = fileInfo.CreationTime
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create dump for database '{databaseName}': {ex.Message}", ex);
            }
        }

        public async Task ImportDumpAsync(string dumpFilePath, string targetDatabase)
        {
            if (string.IsNullOrWhiteSpace(dumpFilePath))
                throw new ArgumentException("Dump file path cannot be empty.", nameof(dumpFilePath));

            if (string.IsNullOrWhiteSpace(targetDatabase))
                throw new ArgumentException("Target database cannot be empty.", nameof(targetDatabase));

            if (!await _fileSystemService.FileExistsAsync(dumpFilePath))
                throw new FileNotFoundException($"Dump file not found: {dumpFilePath}");

            try
            {
                // TODO: Implement actual database import logic based on database type
                // For now, just verify the file can be read
                var content = await _fileSystemService.ReadAllTextAsync(dumpFilePath);
                if (string.IsNullOrEmpty(content))
                {
                    throw new InvalidOperationException("Dump file is empty or cannot be read.");
                }

                System.Diagnostics.Debug.WriteLine($"Importing dump from {dumpFilePath} to database {targetDatabase}");
                // Actual import logic would go here
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to import dump from '{dumpFilePath}' to database '{targetDatabase}': {ex.Message}", ex);
            }
        }

        public async Task DeleteDumpAsync(string dumpFilePath)
        {
            if (string.IsNullOrWhiteSpace(dumpFilePath))
                throw new ArgumentException("Dump file path cannot be empty.", nameof(dumpFilePath));

            if (!await _fileSystemService.FileExistsAsync(dumpFilePath))
                throw new FileNotFoundException($"Dump file not found: {dumpFilePath}");

            try
            {
                await _fileSystemService.DeleteFileAsync(dumpFilePath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to delete dump file '{dumpFilePath}': {ex.Message}", ex);
            }
        }

        public async Task<bool> ValidateDumpFileAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            if (!await _fileSystemService.FileExistsAsync(filePath))
                return false;

            try
            {
                // Basic validation - check if it's a SQL file and not empty
                if (!Path.GetExtension(filePath).Equals(".sql", StringComparison.OrdinalIgnoreCase))
                    return false;

                var fileInfo = await _fileSystemService.GetFileInfoAsync(filePath);
                if (fileInfo.Length == 0)
                    return false;

                // Check if file contains some SQL-like content
                var content = await _fileSystemService.ReadAllTextAsync(filePath);
                return content.Contains("--") || content.Contains("CREATE") || content.Contains("INSERT") || content.Contains("UPDATE");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error validating dump file {filePath}: {ex.Message}");
                return false;
            }
        }
    }
}
