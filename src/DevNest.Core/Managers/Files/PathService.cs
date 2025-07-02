namespace DevNest.Core.Files
{
    public static class PathManager
    {
        public static string BasePath => (System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName is string exePath && Path.GetDirectoryName(exePath) is string dir) ? dir : throw new InvalidOperationException("Could not determine the application's base directory.");
        public static string BinPath => Path.Combine(BasePath, "bin");
        public static string ConfigPath => Path.Combine(BasePath, "config");
        public static string DataPath => Path.Combine(BasePath, "data");
        public static string EtcPath => Path.Combine(BasePath, "etc");
        public static string LogsPath => Path.Combine(BasePath, "logs");
        public static string TemplatesPath => Path.Combine(BasePath, "templates");
        public static string WwwPath => Path.Combine(BasePath, "www");
    }
}
