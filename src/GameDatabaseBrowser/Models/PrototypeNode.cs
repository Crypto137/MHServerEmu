using System.Collections.ObjectModel;

namespace GameDatabaseBrowser.Models
{
    public class PrototypeNode
    {
        public bool IsSelected { get; set; }
        public bool IsExpanded { get; set; }
        public PrototypeDetails PrototypeDetails { get; set; }

        public ObservableCollection<PrototypeNode> Childs { get; set; }

        public PrototypeNode()
        {
            Childs = new ObservableCollection<PrototypeNode>();
        }
    }
}
