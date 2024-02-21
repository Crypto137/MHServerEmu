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
        public List<PrototypeNode> Childs { get; set; }
        public string Name { get; set; }
        public List<Property> Properties { get; set; }

        public PrototypeNode()
        {
            Childs = new List<PrototypeNode>();
        }


    }
}
