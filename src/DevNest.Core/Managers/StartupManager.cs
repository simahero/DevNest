using DevNest.Core.Helpers;
using Microsoft.Win32;
using System;
using System.IO;

namespace DevNest.Core
{
    public class StartupManager
    {
        private const string RunKey = @"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string AppName = "DevNest";

        public StartupManager() { }

        public async Task CopyStarterDirOnStartup()
        {
            string exePath = PathManager.BasePath;

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
                    break;
                }
            }

            try
            {
                if (sourceDir != null)
                {
                    await FileSystemManager.CopyDirectory(sourceDir, exePath, true);
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
            string[] servers = { "apache", "nginx" };

            string templateDir = PathManager.TemplatesPath;


            foreach (var server in servers)
            {
                string aliasDir = Path.Combine(PathManager.EtcPath, server, "alias");
                if (!Directory.Exists(aliasDir))
                {
                    _ = Directory.CreateDirectory(aliasDir);
                }

                var templateFiles = Directory.GetFiles(templateDir, "alias*.tpl").Where(f => f.Contains(server)).ToArray();

                foreach (var tplFile in templateFiles)
                {
                    var templateContent = await FileSystemManager.ReadAllTextAsync(tplFile);
                    var rootPath = PathManager.BasePath.Replace('\\', '/');
                    var configContent = templateContent.Replace("<<ROOT>>", rootPath);

                    var tplFileName = Path.GetFileName(tplFile);
                    var prefix = $"alias.{server}.";
                    string configFileName = tplFileName.Replace(prefix, "").Replace(".tpl", "");
                    var configFilePath = Path.Combine(aliasDir, configFileName);

                    await FileSystemManager.WriteAllTextAsync(configFilePath, configContent);
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
