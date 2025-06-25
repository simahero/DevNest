namespace DevNest.Core.Models
{
    public class InstallationResultModel
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? InstalledPath { get; set; }
        public Exception? Exception { get; set; }
    }

}
