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

        public PrototypeDetails PrototypeDetails { get; set; }

        public ObservableCollection<PrototypeNode> Childs { get; set; }

        public PrototypeNode()
        {
            Childs = new ObservableCollection<PrototypeNode>();
        }
    }
}
