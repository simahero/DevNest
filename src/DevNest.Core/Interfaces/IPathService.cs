namespace DevNest.Core.Interfaces
{
    /// <summary>
    /// Service responsible for providing crucial system paths instead of hardcoding them.
    /// </summary>
    public interface IPathService
    {
        /// <summary>
        /// Gets the path to the bin directory.
        /// </summary>
        string BinPath { get; }

        /// <summary>
        /// Gets the path to the www directory.
        /// </summary>
        string WwwPath { get; }

        /// <summary>
        /// Gets the path to the config directory.
        /// </summary>
        string ConfigPath { get; }

        /// <summary>
        /// Gets the path to the sites-enabled directory.
        /// </summary>
        string SitesEnabledPath { get; }

        /// <summary>
        /// Gets the base application directory path.
        /// </summary>
        string BasePath { get; }

        /// <summary>
        /// Ensures that all crucial directories exist, creating them if necessary.
        /// </summary>
        void EnsureDirectoriesExist();

        /// <summary>
        /// Gets a path relative to the base application directory.
        /// </summary>
        /// <param name="relativePath">The relative path to combine with the base path.</param>
        /// <returns>The full path.</returns>
        string GetPath(string relativePath);
    }
}
