namespace DevNest.Core.Files
{
    public class PathManager
    {
        private readonly string _basePath;

        public PathManager()
        {
            // Use the application's base directory as the root
            // _basePath = @"C:\DevNest";
            var exePath = (System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName) ?? throw new InvalidOperationException("Could not determine the application's executable path.");
            var dir = Path.GetDirectoryName(exePath) ?? throw new InvalidOperationException("Could not determine the application's base directory.");
            _basePath = dir;
        }

        public PathManager(string basePath)
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


        public void EnsureDirectoriesExist()
        {
            EnsureDirectoryExists(BinPath);
            EnsureDirectoryExists(ConfigPath);
            EnsureDirectoryExists(DataPath);
            EnsureDirectoryExists(EtcPath);
            EnsureDirectoryExists(LogsPath);
            EnsureDirectoryExists(TemplatesPath);
            EnsureDirectoryExists(WwwPath);
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
