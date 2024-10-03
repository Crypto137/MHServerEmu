using System;
using System.Collections.Generic;
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

        public bool SearchText(string text, List<PropertyNode> matches)
        {
            bool found = PropertyDetails.ToString().Contains(text, StringComparison.OrdinalIgnoreCase);
            if (found)
                matches.Add(this);

            foreach (PropertyNode child in Childs)
                child.SearchText(text, matches);

            return found;
        }
    }
}
