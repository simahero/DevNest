using DevNest.Core.Interfaces;

namespace DevNest.Services
{
    public class StartupManager
    {
        private readonly IFileSystemService _fileSystemService;
        private readonly IPathService _pathService;
        private readonly LogManager _logManager;

        public StartupManager(IFileSystemService fileSystemService, IPathService pathService, LogManager logManager)
        {
            _fileSystemService = fileSystemService;
            _pathService = pathService;
            _logManager = logManager;

        }

        public Task CopyStarterDirOnStartup()
        {

            if (_pathService == null)
            {
                _logManager?.Log("IPathService is not available.");
                return Task.CompletedTask;
            }

            string exePath = _pathService.BasePath;

            string[] possibleSources = new[]
            {
                Path.Combine(exePath, "Assets", "starter_dirs"), // For Debug or non-single-file
                Path.Combine(AppContext.BaseDirectory, "Assets", "starter_dirs") // For single-file publish
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

            _logManager?.Log($"Copying starter_dirs from {sourceDir ?? "<not found>"} to {exePath}");

            try
            {
                if (sourceDir != null)
                {
                    _fileSystemService.CopyDirectory(sourceDir, exePath, overwrite: true);
                }
                else
                {
                    _logManager?.Log($"Source starter_dirs not found in any known location.");
                }
            }
            catch (Exception ex)
            {
                _logManager?.Log($"Failed to copy starter_dirs: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        public async Task EnsureAliasConfs()
        {
            string[] servers = { "apache", "nginx" };

            string templateDir = _pathService.TemplatesPath;


            foreach (var server in servers)
            {
                string aliasDir = Path.Combine(_pathService.EtcPath, server, "alias");
                if (!Directory.Exists(aliasDir))
                {
                    _logManager.Log($"{server} alias configs missing, creating them...");
                    Directory.CreateDirectory(aliasDir);
                }

                var templateFiles = Directory.GetFiles(templateDir, "alias*.tpl").Where(f => f.Contains(server)).ToArray();

                foreach (var tplFile in templateFiles)
                {
                    var templateContent = await _fileSystemService.ReadAllTextAsync(tplFile);
                    var rootPath = _pathService.BasePath.Replace('\\', '/');
                    var configContent = templateContent.Replace("<<ROOT>>", rootPath);

                    var tplFileName = Path.GetFileName(tplFile);
                    var prefix = $"alias.{server}.";
                    string configFileName = tplFileName.Replace(prefix, "").Replace(".tpl", "");
                    var configFilePath = Path.Combine(aliasDir, configFileName);

                    _logManager.Log($"Creating {server} alias {configFilePath}");

                    await _fileSystemService.WriteAllTextAsync(configFilePath, configContent);
                }
            }


        }

    }
}
