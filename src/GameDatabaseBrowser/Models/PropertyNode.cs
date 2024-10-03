using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GameDatabaseBrowser.Models
{
    public class PropertyNode
    {
        private bool _isSearchMatch = false;

        public bool IsSelected { get; set; }
        public bool IsExpanded { get; set; }

        public string Background { get => _isSearchMatch ? "#FFFDE8BA" : "#FFFFFF"; }
        public bool CollapseOnSearchClear { get; set; } = true;

        public PropertyDetails PropertyDetails { get; set; }

        public ObservableCollection<PropertyNode> Childs { get; set; }

        public PropertyNode()
        {
            Childs = new ObservableCollection<PropertyNode>();
        }

        public bool SearchText(string text, List<PropertyNode> matches)
        {
            _isSearchMatch = PropertyDetails.ToString().Contains(text, StringComparison.OrdinalIgnoreCase);
            if (_isSearchMatch)
                matches.Add(this);

            foreach (PropertyNode child in Childs)
            {
                if (child.SearchText(text, matches))
                {
                    _isSearchMatch = true;
                    IsExpanded = true;
                }
            }

            return _isSearchMatch;
        }

        public void ClearSearch()
        {
            _isSearchMatch = false;

            if (CollapseOnSearchClear)
                IsExpanded = false;

            foreach (PropertyNode child in Childs)
                child.ClearSearch();
        }
    }
}
