using DevNest.Core.Interfaces;

namespace DevNest.Services.Files
{
    public class PathService : IPathService
    {
        private readonly string _basePath;

        public PathService()
        {
            // Use the application's base directory as the root
            _basePath = @"C:\DevNest";
        }

        public PathService(string basePath)
        {
            _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
        }

        public string BasePath => _basePath;

        public string BinPath => Path.Combine(_basePath, "bin");
        public string ConfigPath => Path.Combine(_basePath, "config");
        public string DataPath => Path.Combine(_basePath, "data");
        public string EtcPath => Path.Combine(_basePath, "etc");
        public string LogsPath => Path.Combine(_basePath, "logs");
        public string TemplatesPath => Path.Combine(_basePath, "templates");
        public string WwwPath => Path.Combine(_basePath, "www");

        public string SitesEnabledPath => Path.Combine(_basePath, "etc", "apache", "sites-enabled");

        public void EnsureDirectoriesExist()
        {
            EnsureDirectoryExists(BinPath);
            EnsureDirectoryExists(ConfigPath);
            EnsureDirectoryExists(DataPath);
            EnsureDirectoryExists(EtcPath);
            EnsureDirectoryExists(LogsPath);
            EnsureDirectoryExists(TemplatesPath);
            EnsureDirectoryExists(WwwPath);

            EnsureDirectoryExists(SitesEnabledPath);
        }

        public string GetPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentException("Relative path cannot be null or empty.", nameof(relativePath));
            }

            return Path.Combine(_basePath, relativePath);
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
