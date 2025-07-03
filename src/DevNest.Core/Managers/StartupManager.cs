using DevNest.Core.Helpers;

namespace DevNest.Core
{
    public class StartupManager
    {
        private const string RunKey = @"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string AppName = "DevNest";

        public StartupManager() { }

        public async Task CopyStarterDirOnStartup()
        {
            string exePath = PathHelper.BasePath;

            string[] possibleSources = new[]
            {
                Path.Combine(exePath, "Assets", "Include"),
                Path.Combine(AppContext.BaseDirectory, "Assets", "Include")
            };

            string? sourceDir = null;
            foreach (var src in possibleSources)
            {
                if (Directory.Exists(src))
                {
                    sourceDir = src;

                    _ = Logger.Log($"{sourceDir}");

                    break;
                }
            }

            try
            {
                if (sourceDir != null)
                {
                    await FileSystemHelper.CopyDirectory(sourceDir, exePath, false);
                    _ = Logger.Log($"Copying starting directory.");
                }
                else
                {
                    _ = Logger.Log($"Source Include not found in any known location.");
                }
            }
            catch (Exception ex)
            {
                _ = Logger.Log($"Failed to copy Include: {ex.Message}");
            }
        }

        public async Task EnsureAliasConfs()
        {
            string[] servers = { "apache2", "nginx" };

            string templateDir = PathHelper.TemplatesPath;


            foreach (var server in servers)
            {
                string aliasDir = Path.Combine(PathHelper.EtcPath, server, "alias");
                if (!Directory.Exists(aliasDir))
                {
                    _ = Directory.CreateDirectory(aliasDir);
                }

                var templateFiles = Directory.GetFiles(templateDir, "alias*.tpl").Where(f => f.Contains(server)).ToArray();

                foreach (var tplFile in templateFiles)
                {
                    var templateContent = await FileSystemHelper.ReadAllTextAsync(tplFile);
                    var rootPath = PathHelper.BasePath.Replace('\\', '/');
                    var configContent = templateContent.Replace("<<ROOT>>", rootPath);

                    var tplFileName = Path.GetFileName(tplFile);
                    var prefix = $"alias.{server}.";
                    string configFileName = tplFileName.Replace(prefix, "").Replace(".tpl", "");
                    var configFilePath = Path.Combine(aliasDir, configFileName);

                    await FileSystemHelper.WriteAllTextAsync(configFilePath, configContent);
                }
            }
        }

        public static void EnableStartup()
        {
#if WINDOWS
                    using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey, true))
                    {
                        if (key != null)
                        {
                            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                            key.SetValue(AppName, '"' + exePath + '"');
                        }
                    }
#endif
        }

        public static void DisableStartup()
        {
#if WINDOWS
                    using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey, true))
                    {
                        if (key != null)
                        {
                            key.DeleteValue(AppName, false);
                        }
                    }
#endif
        }

        public static bool IsStartupEnabled()
        {
#if WINDOWS
                    using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey, false))
                    {
                        if (key != null)
                        {
                            var value = key.GetValue(AppName);
                            return value != null;
                        }
                    }
#endif
            return false;
        }

    }
}
