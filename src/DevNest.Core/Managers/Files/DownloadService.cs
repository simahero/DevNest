using System.Net;
using System.Net.Http.Headers;

namespace DevNest.Core.Files
{
    public class DownloadManager
    {
        private static readonly HttpClient _httpClient;

        static DownloadManager()
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                UseCookies = true,
                CookieContainer = new CookieContainer()
            };

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMinutes(10)
            };

            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            _httpClient.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            _httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.5");

            // Important: Pretend we clicked the download link from the MySQL website
            _httpClient.DefaultRequestHeaders.Referrer = new Uri("https://dev.mysql.com/downloads/mysql/");
        }

        public async Task<string> DownloadToTempAsync(string url, IProgress<string>? progress = null)
        {
            progress?.Report($"Downloading from: {url}...");

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"HTTP error while downloading {url}: {ex.Message}");
            }

            var finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? url;
            if ((int)response.StatusCode == 403)
            {
                throw new Exception($"Access forbidden (403) for: {finalUrl}. The server may require referrer or session cookies.");
            }

            var fileName = GetFileName(response.Content.Headers, url);
            var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_{fileName}");

            using var stream = await response.Content.ReadAsStreamAsync();

            // Read to memory to inspect for HTML errors
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();

            if (bytes.Length < 100)
                throw new Exception("Downloaded file is too small - likely an error page.");

            var contentStart = System.Text.Encoding.UTF8.GetString(bytes.Take(100).ToArray()).ToLowerInvariant();
            if (contentStart.Contains("<html") || contentStart.Contains("error") || contentStart.Contains("403"))
                throw new Exception("Downloaded content appears to be an HTML error page (403).");

            await File.WriteAllBytesAsync(tempPath, bytes);

            progress?.Report($"Downloaded {fileName} ({bytes.Length / 1024.0 / 1024.0:F1} MB)");

            return tempPath;
        }

        private static string GetFileName(HttpContentHeaders headers, string url)
        {
            if (headers.ContentDisposition?.FileNameStar != null)
                return headers.ContentDisposition.FileNameStar.Trim('"');

            if (headers.ContentDisposition?.FileName != null)
                return headers.ContentDisposition.FileName.Trim('"');

            var uri = new Uri(url);
            var fallbackName = Path.GetFileName(uri.LocalPath);
            return string.IsNullOrEmpty(fallbackName) || !fallbackName.Contains('.')
                ? $"download_{DateTime.Now:yyyyMMddHHmmss}.zip"
                : fallbackName;
        }
    }
}
