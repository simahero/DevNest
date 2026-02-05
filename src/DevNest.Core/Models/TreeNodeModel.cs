using System.Collections.ObjectModel;

namespace DevNest.UI.Models
{
    public class TreeNodeModel
    {
        public string Content { get; set; } = string.Empty;
        public bool IsExpanded { get; set; } = false;
        public ObservableCollection<TreeNodeModel> Children { get; set; } = new();
    }
}
