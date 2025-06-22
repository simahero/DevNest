using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace DevNest.Controllers
{
    public class DownloadController
    {
        public async Task<string> DownloadToTempAsync(string url, IProgress<string>? progress = null)
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(10);

            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");

            var fileName = GetFileNameFromUrl(url);

            progress?.Report($"Downloading from: {url}...");

            HttpResponseMessage response;
            try
            {
                response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? url;
                if (finalUrl.Contains("error") || finalUrl.Contains("404") || finalUrl.Contains("not-found"))
                {
                    throw new Exception($"Download URL appears to be invalid or file not found: {finalUrl}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to download from {url}: {ex.Message}");
            }

            if (response.Content.Headers.ContentDisposition?.FileName != null)
            {
                fileName = response.Content.Headers.ContentDisposition.FileName.Trim('"');
            }

            var contentBytes = await response.Content.ReadAsByteArrayAsync();

            if (contentBytes.Length < 100)
            {
                throw new Exception("Downloaded file is too small - likely an error page or redirect");
            }

            var contentStart = System.Text.Encoding.UTF8.GetString(contentBytes.Take(100).ToArray()).ToLower();
            if (contentStart.Contains("<html") ||
                contentStart.Contains("<!doctype") ||
                contentStart.Contains("<title>") ||
                contentStart.Contains("error") ||
                contentStart.Contains("not found") ||
                contentStart.Contains("404"))
            {
                throw new Exception("Downloaded content appears to be an HTML error page instead of the expected file");
            }

            var sizeInMB = contentBytes.Length / 1024.0 / 1024.0;
            progress?.Report($"Downloading {fileName} ({sizeInMB:F1} MB)");

            var tempFilePath = Path.Combine(Path.GetTempPath(), fileName);
            await File.WriteAllBytesAsync(tempFilePath, contentBytes);

            return tempFilePath;
        }

        private string GetFileNameFromUrl(string url)
        {
            var uri = new Uri(url);
            var fileName = Path.GetFileName(uri.LocalPath);

            if (string.IsNullOrEmpty(fileName) || !fileName.Contains('.'))
            {
                // If we can't determine the filename, use a default based on the URL
                fileName = $"download_{DateTime.Now:yyyyMMddHHmmss}.zip";
            }

            return fileName;
        }

        public async Task ExtractAsync(string filePath, string destination, bool hasAdditionalDir, IProgress<string>? progress = null)
        {
            var fileName = Path.GetFileName(filePath);
            progress?.Report($"Extracting {fileName}...");

            if (IsArchive(filePath))
            {
                await ExtractZipFile(filePath, destination, hasAdditionalDir, progress);
            }
            else
            {
                if (!Directory.Exists(destination))
                {
                    Directory.CreateDirectory(destination);
                }

                var destinationFile = Path.Combine(destination, fileName);
                File.Copy(filePath, destinationFile, true);
                progress?.Report($"Copied {fileName} to {destination}");
            }
        }

        private async Task ExtractZipFile(string filePath, string destination, bool hasAdditionalDir, IProgress<string>? progress = null)
        {
            var tempExtractPath = Path.Combine(Path.GetTempPath(), $"devnest_extract_{Guid.NewGuid()}");

            try
            {
                await Task.Run(() =>
                {
                    ZipFile.ExtractToDirectory(filePath, tempExtractPath);

                    var extractedDirs = Directory.GetDirectories(tempExtractPath);
                    var extractedFiles = Directory.GetFiles(tempExtractPath);

                    if (!Directory.Exists(destination))
                    {
                        Directory.CreateDirectory(destination);
                    }

                    if (hasAdditionalDir)
                    {
                        var sourceDir = extractedDirs[0];
                        var subDirs = Directory.GetDirectories(sourceDir);
                        var subFiles = Directory.GetFiles(sourceDir);

                        progress?.Report("Moving extracted contents...");

                        foreach (var dir in subDirs)
                        {
                            var dirName = Path.GetFileName(dir);
                            var targetDir = Path.Combine(destination, dirName);
                            if (Directory.Exists(targetDir))
                            {
                                Directory.Delete(targetDir, true);
                            }
                            Directory.Move(dir, targetDir);
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

        private static readonly string[] ArchiveExts = [".zip", ".rar", ".7z", ".tar", ".tar.gz", ".tgz", ".tar.bz2", ".tbz2"];

        private static bool IsArchive(string path)
        {
            string fileName = Path.GetFileName(path);
            return ArchiveExts.Any(e => fileName.EndsWith(e, StringComparison.OrdinalIgnoreCase));
        }



    }
}
