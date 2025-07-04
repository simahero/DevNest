namespace DevNest.Core.Helpers
{
    public static class PathHelper
    {
        private static bool useWSL = false;

        public static void SetUseWSL(bool value)
        {
            useWSL = value;
        }

        public static string BasePath
        {
            get
            {
                if (useWSL)
                {
                    return "\\\\wsl.localhost\\Ubuntu\\home\\zoile\\DevNest";
                }

                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;

                if (exePath is string path && Path.GetDirectoryName(path) is string dir)
                {
                    return dir;
                }

                throw new InvalidOperationException("Could not determine the application's base directory.");
            }
        }

        public static string WinPath
        {
            get
            {
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;

                if (exePath is string path && Path.GetDirectoryName(path) is string dir)
                {
                    return dir;
                }

                throw new InvalidOperationException("Could not determine the application's base directory.");
            }
        }

        public static string BaseSettingsPath
        {
            get
            {
                return Path.Combine(ConfigPath, "settings.ini");
            }
        }

        public static string SettingsPath
        {
            get
            {
                if (useWSL)
                {
                    return Path.Combine(ConfigPath, "wsl.settings.ini");
                }

                return Path.Combine(ConfigPath, "win.settings.ini");
            }
        }

        public static string BinPath => Path.Combine(BasePath, "bin");
        public static string ConfigPath => Path.Combine(WinPath, "config");
        public static string DataPath => Path.Combine(BasePath, "data");
        public static string EtcPath => Path.Combine(BasePath, "etc");
        public static string LogsPath => Path.Combine(BasePath, "logs");
        public static string TemplatesPath => Path.Combine(BasePath, "templates");
        public static string WwwPath => Path.Combine(BasePath, "www");
    }
}
