namespace DevNest.Core.Interfaces
{
    public interface IArchiveExtractionService
    {
        Task ExtractAsync(string filePath, string destination, bool hasAdditionalDir, IProgress<string>? progress = null);
        bool IsArchive(string path);
    }
}
