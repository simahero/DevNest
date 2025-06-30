using DevNest.Core.Files;

namespace DevNest.Core
{
    public class StartupManager
    {
        private readonly FileSystemManager _fileSystemManager;
        private readonly PathManager _pathManager;
        private readonly LogManager _logManager;

        public StartupManager(FileSystemManager fileSystemManager, PathManager pathManager, LogManager logManager)
        {
            _fileSystemManager = fileSystemManager;
            _pathManager = pathManager;
            _logManager = logManager;

        }

        public async Task CopyStarterDirOnStartup()
        {
            if (_pathManager == null)
            {
                _logManager?.Log("IPathService is not available.");
                return;
            }

            string exePath = _pathManager.BasePath;

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
                    await _fileSystemManager.CopyDirectory(sourceDir, exePath, overwrite: false);
                }
                else
                {
                    _logManager?.Log($"Source Include not found in any known location.");
                }
            }
            catch (Exception ex)
            {
                _logManager?.Log($"Failed to copy Include: {ex.Message}");
            }
        }

        public async Task EnsureAliasConfs()
        {
            string[] servers = { "apache", "nginx" };

            string templateDir = _pathManager.TemplatesPath;


            foreach (var server in servers)
            {
                string aliasDir = Path.Combine(_pathManager.EtcPath, server, "alias");
                if (!Directory.Exists(aliasDir))
                {
                    Directory.CreateDirectory(aliasDir);
                }

                var templateFiles = Directory.GetFiles(templateDir, "alias*.tpl").Where(f => f.Contains(server)).ToArray();

                foreach (var tplFile in templateFiles)
                {
                    var templateContent = await _fileSystemManager.ReadAllTextAsync(tplFile);
                    var rootPath = _pathManager.BasePath.Replace('\\', '/');
                    var configContent = templateContent.Replace("<<ROOT>>", rootPath);

                    var tplFileName = Path.GetFileName(tplFile);
                    var prefix = $"alias.{server}.";
                    string configFileName = tplFileName.Replace(prefix, "").Replace(".tpl", "");
                    var configFilePath = Path.Combine(aliasDir, configFileName);

                    await _fileSystemManager.WriteAllTextAsync(configFilePath, configContent);
                }
            }


        }

    }
}
