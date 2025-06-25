using DevNest.Core.Interfaces;
using DevNest.Core.Models;

namespace DevNest.Services.Sites
{
    public class SiteUrlDetectionService : ISiteUrlDetectionService
    {
        private readonly IFileSystemService _fileSystemService;

        public SiteUrlDetectionService(IFileSystemService fileSystemService)
        {
            _fileSystemService = fileSystemService;
        }

        public async Task<string> DetectSiteUrlAsync(SiteModel site)
        {
            // Check for common development server configurations
            var packageJsonPath = Path.Combine(site.Path, "package.json");

            if (await _fileSystemService.FileExistsAsync(packageJsonPath))
            {
                // Check if it's a React, Vue, or other Node.js app
                var packageContent = await _fileSystemService.ReadAllTextAsync(packageJsonPath);

                if (packageContent.Contains("react-scripts") || packageContent.Contains("create-react-app"))
                {
                    return "http://localhost:3000"; // Default React dev server
                }
                else if (packageContent.Contains("vue") || packageContent.Contains("@vue/cli"))
                {
                    return "http://localhost:8080"; // Default Vue dev server
                }
                else if (packageContent.Contains("next"))
                {
                    return "http://localhost:3000"; // Default Next.js dev server
                }
                else if (packageContent.Contains("express"))
                {
                    return "http://localhost:3000"; // Default Express server
                }
            }

            // Check for Laravel
            var laravelPath = Path.Combine(site.Path, "artisan");
            if (await _fileSystemService.FileExistsAsync(laravelPath))
            {
                return "http://localhost:8000"; // Default Laravel dev server
            }

            // Check for PHP files
            var phpFiles = Directory.GetFiles(site.Path, "*.php", SearchOption.TopDirectoryOnly);
            if (phpFiles.Length > 0)
            {
                return "http://localhost"; // Generic PHP server
            }

            // Check for static HTML
            var indexHtml = Path.Combine(site.Path, "index.html");
            if (await _fileSystemService.FileExistsAsync(indexHtml))
            {
                return $"file:///{site.Path.Replace('\\', '/')}/index.html";
            }

            // Default fallback
            return site.Url ?? $"http://{site.Name}.test";
        }
    }
}
