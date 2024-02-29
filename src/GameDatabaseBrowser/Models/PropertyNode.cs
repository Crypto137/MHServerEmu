using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameDatabaseBrowser.Models
{
    public class PropertyNode
    {
        public bool IsSelected { get; set; }

        public PropertyDetails PropertyDetails { get; set; }

        public ObservableCollection<PropertyNode> Childs { get; set; }

        public PropertyNode()
        {
            Childs = new ObservableCollection<PropertyNode>();
        }
    }
}
