using AutoCompleteTextBox.Editors;
using MHServerEmu.Games.GameData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameDatabaseBrowser.Providers
{
    /// <summary>
    /// Blueprint list provider for autocompletion feature
    /// </summary>
    internal class PrototypeBlueprintProvider : ISuggestionProvider
    {
        private readonly List<string> _prototypeBlueprintList = new();

        public PrototypeBlueprintProvider()
        {
            InitPrototypeBlueprintList();
        }

        private void InitPrototypeBlueprintList()
        {
            foreach (var blueprint in GameDatabase.DataDirectory.IterateBlueprints())
                _prototypeBlueprintList.Add(blueprint.ToString());
        }

        public IEnumerable GetSuggestions(string filter)
        {
            return _prototypeBlueprintList.Where(x => x.ToLowerInvariant().Contains(filter.ToLowerInvariant())).OrderBy(k => k);

        }
    }
}
