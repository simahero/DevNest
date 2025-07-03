namespace DevNest.Core.Helpers
{
    public class ArchiveHelper
    {
        private static readonly string[] ArchiveExts = [".zip", ".rar", ".7z", ".tar", ".tar.gz", ".tgz", ".tar.bz2", ".tbz2"];

        public static async Task ExtractAsync(string filePath, string destination, bool hasAdditionalDir, IProgress<string>? progress = null)
        {
            var fileName = Path.GetFileName(filePath);
            progress?.Report($"Extracting {fileName}...");

            if (IsArchive(filePath))
            {
                await ExtractZipFileAsync(filePath, destination, hasAdditionalDir, progress);
            }
            else
            {
                if (!Directory.Exists(destination))
                {
                    Directory.CreateDirectory(destination);
                }

                var destinationFile = Path.Combine(destination, fileName);
                File.Copy(filePath, destinationFile);
                progress?.Report($"Copied {fileName} to {destination}");
            }
        }

        public static bool IsArchive(string path)
        {
            string fileName = Path.GetFileName(path);
            return ArchiveExts.Any(e => fileName.EndsWith(e, StringComparison.OrdinalIgnoreCase));
        }

        private static async Task ExtractZipFileAsync(string filePath, string destination, bool hasAdditionalDir, IProgress<string>? progress = null)
        {
            var tempExtractPath = Path.Combine(Path.GetTempPath(), $"devnest_extract_{Guid.NewGuid()}");

            try
            {
                await Task.Run(async () =>
                {
                    System.IO.Compression.ZipFile.ExtractToDirectory(filePath, tempExtractPath);

                    var extractedDirs = Directory.GetDirectories(tempExtractPath);
                    var extractedFiles = Directory.GetFiles(tempExtractPath);

                    if (!Directory.Exists(destination))
                    {
                        Directory.CreateDirectory(destination);
                    }

                    if (hasAdditionalDir && extractedDirs.Length > 0)
                    {
                        var sourceDir = extractedDirs[0];
                        var subDirs = Directory.GetDirectories(sourceDir);
                        var subFiles = Directory.GetFiles(sourceDir);

                        progress?.Report("Moving extracted contents...");

                        foreach (var dir in subDirs)
                        {
                            var dirName = Path.GetFileName(dir);
                            var targetDir = Path.Combine(destination, dirName);
                            await FileSystemHelper.CopyDirectory(dir, targetDir, true);
                        }

                        foreach (var file in subFiles)
                        {
                            var fileName = Path.GetFileName(file);
                            var targetFile = Path.Combine(destination, fileName);
                            if (File.Exists(targetFile))
                            {
                                File.Delete(targetFile);
                            }
                            File.Move(file, targetFile);
                        }
                    }
                    else
                    {
                        progress?.Report("Moving extracted files...");

                        foreach (var dir in extractedDirs)
                        {
                            var dirName = Path.GetFileName(dir);
                            var targetDir = Path.Combine(destination, dirName);
                            if (Directory.Exists(targetDir))
                            {
                                Directory.Delete(targetDir, true);
                            }
                            Directory.Move(dir, targetDir);
                        }

                        foreach (var file in extractedFiles)
                        {
                            var fileName = Path.GetFileName(file);
                            var targetFile = Path.Combine(destination, fileName);
                            if (File.Exists(targetFile))
                            {
                                File.Delete(targetFile);
                            }
                            File.Move(file, targetFile);
                        }
                    }
                });

                progress?.Report("Extraction completed successfully");
            }
            catch (InvalidDataException ex)
            {
                throw new Exception($"Archive appears to be corrupted: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Extraction failed: {ex.Message}");
            }
            finally
            {
                if (Directory.Exists(tempExtractPath))
                {
                    Directory.Delete(tempExtractPath, true);
                }
            }
        }
    }
}
