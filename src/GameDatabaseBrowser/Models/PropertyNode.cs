using System.Collections.ObjectModel;

namespace GameDatabaseBrowser.Models
{
    public class PropertyNode
    {
        public bool IsSelected { get; set; }
        public bool IsExpanded { get; set; }

        public PropertyDetails PropertyDetails { get; set; }

        public ObservableCollection<PropertyNode> Childs { get; set; }

        public PropertyNode()
        {
            Childs = new ObservableCollection<PropertyNode>();
        }
    }
}
