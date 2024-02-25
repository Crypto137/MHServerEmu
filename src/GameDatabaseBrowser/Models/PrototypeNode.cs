using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GameDatabaseBrowser.Models
{
    public class PrototypeNode
    {
        public bool IsSelected { get; set; }
        public bool IsExpanded { get; set; }

        public ObservableCollection<PrototypeNode> Childs { get; set; }
        public string Name { get; set; }
        public List<Property> Properties { get; set; }

        public PrototypeNode()
        {
            Childs = new ObservableCollection<PrototypeNode>();
        }
    }
}
