using AutoCompleteTextBox.Editors;
using MHServerEmu.Games.GameData.Prototypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GameDatabaseBrowser.Providers
{
    /// <summary>
    /// Prototype class list provider for autocompletion feature
    /// </summary>
    public class PrototypeClassProvider : ISuggestionProvider
    {
        private readonly List<string> _prototypeClassList = new();

        public PrototypeClassProvider()
        {
            InitPrototypeClassList();
        }

        /// <summary>
        /// List all the classes that inherits the prototype class
        /// </summary>
        private void InitPrototypeClassList()
        {
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().Where(k => k.FullName.Contains("MHServerEmu")).First();
            var derivedTypes = assembly.GetTypes().Where(t => t.IsClass && t.IsSubclassOf(typeof(Prototype)));

            foreach (var type in derivedTypes)
                _prototypeClassList.Add(type.Name);
        }

        public IEnumerable GetSuggestions(string filter)
        {
            return _prototypeClassList.Where(x => x.ToLowerInvariant().Contains(filter.ToLowerInvariant())).OrderBy(k=>k);
        }
    }
}
