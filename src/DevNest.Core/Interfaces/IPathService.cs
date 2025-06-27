namespace DevNest.Core.Interfaces
{
    public interface IPathService
    {
        string BinPath { get; }

        string WwwPath { get; }

        string ConfigPath { get; }

        string DataPath { get; }

        string EtcPath { get; }

        string LogsPath { get; }

        string TemplatesPath { get; }

        string BasePath { get; }

        void EnsureDirectoriesExist();

        string GetPath(string relativePath);
    }
}
