namespace DevNest.Core.Models
{
    public class PhpExtension
    {
        public string Name { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string OriginalLine { get; set; } = string.Empty;
    }
}
