using GameDatabaseBrowser.Models;
using MHServerEmu.Games.GameData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace GameDatabaseBrowser.Helpers
{
    public class SearchHelper
    {
        readonly MainWindow _mainWindow;

        public SearchHelper(MainWindow mainWindow) => _mainWindow = mainWindow;

        /// <summary>
        /// Filter the prototype to display as result
        /// </summary>
        public List<PrototypeDetails> GetPrototypeToDisplay()
        {
            return _mainWindow.SearchTypeComboBox.SelectedIndex switch
            {
                // Search by text
                0 => GetPrototypeToDisplayFromTextSearch(),
                // Search by class
                1 => GetPrototypeToDisplayFromClassSearch(),
                // Search by blueprint
                2 => GetPrototypeToDisplayFromBlueprintSearch(),
                _ => null,
            };
        }

        /// <summary>
        /// Filter the prototype to display as result considering prototype blueprint search
        /// </summary>
        private List<PrototypeDetails> GetPrototypeToDisplayFromBlueprintSearch()
        {
            List<PrototypeDetails> prototypeToDisplay = new();
            if (string.IsNullOrWhiteSpace(_mainWindow.blueprintAutoCompletionText.Text))
                return prototypeToDisplay;

            BlueprintId blueprintId = GameDatabase.BlueprintRefManager.GetDataRefByName(_mainWindow.blueprintAutoCompletionText.Text);

            if (blueprintId == BlueprintId.Invalid)
                return prototypeToDisplay;

            foreach (PrototypeId prototypeId in GameDatabase.DataDirectory.IteratePrototypesInHierarchy(blueprintId, GetPrototypeIterateFlags()))
                prototypeToDisplay.Add(_mainWindow.PrototypeDetails.Where(k => k.FullName == GameDatabase.GetPrototypeName(prototypeId)).First());

            return prototypeToDisplay;
        }

        /// <summary>
        /// Filter the prototype to display as result considering prototype class search
        /// </summary>
        private List<PrototypeDetails> GetPrototypeToDisplayFromClassSearch()
        {
            List<PrototypeDetails> prototypeToDisplay = new();
            if (string.IsNullOrWhiteSpace(_mainWindow.classAutoCompletionText.Text))
                return prototypeToDisplay;

            Type classType = AppDomain.CurrentDomain.GetAssemblies().Where(k => k.FullName.Contains("MHServerEmu"))
                .SelectMany(asm => asm.GetTypes()).FirstOrDefault(type => type.Name == _mainWindow.classAutoCompletionText.Text);

            if (classType == null)
                return prototypeToDisplay;

            foreach (PrototypeId prototypeId in GameDatabase.DataDirectory.IteratePrototypesInHierarchy(classType, GetPrototypeIterateFlags()))
                prototypeToDisplay.Add(_mainWindow.PrototypeDetails.Where(k => k.FullName == GameDatabase.GetPrototypeName(prototypeId)).First());

            return prototypeToDisplay;
        }

        /// <summary>
        /// Prepare for prototype iteration
        /// </summary>
        public PrototypeIterateFlags GetPrototypeIterateFlags()
        {
            PrototypeIterateFlags flags = PrototypeIterateFlags.None;
            if (_mainWindow.abstractClassToggle.IsChecked != true)
                flags |= PrototypeIterateFlags.NoAbstract;

            if (_mainWindow.notApprovedClassToggle.IsChecked != true)
                flags |= PrototypeIterateFlags.ApprovedOnly;

            return flags;
        }

        /// <summary>
        /// Filter the prototype to display as result considering text search
        /// </summary>
        private List<PrototypeDetails> GetPrototypeToDisplayFromTextSearch()
        {
            string currentFilter = "";
            if (ulong.TryParse(_mainWindow.txtSearch.Text, out ulong prototypeId))
                currentFilter = GameDatabase.GetPrototypeName((PrototypeId)prototypeId).ToLowerInvariant();
            else
                currentFilter = _mainWindow.txtSearch.Text.ToLowerInvariant();

            if (string.IsNullOrEmpty(currentFilter))
                return _mainWindow.PrototypeDetails;

            if (_mainWindow.referencesToggle.IsChecked == true)
            {
                List<PrototypeDetails> prototypeToDisplay = new();
                PrototypeId key = GameDatabase.GetPrototypeRefByName(currentFilter);
                if (key != 0 && _mainWindow.CacheDictionary.ContainsKey(key))
                {
                    List<PrototypeId> cachePrototypeIds = _mainWindow.CacheDictionary[key];
                    foreach (PrototypeId cachePrototypeId in cachePrototypeIds)
                        prototypeToDisplay.Add(_mainWindow.PrototypeDetails.Where(k => k.FullName == GameDatabase.GetPrototypeName(cachePrototypeId)).First());
                }

                return prototypeToDisplay;
            }
            else
            {
                bool exactSearch = _mainWindow.exactMatchToggle.IsChecked ?? false;
                if (exactSearch)
                    return _mainWindow.PrototypeDetails.Where(k => k.FullName.ToLowerInvariant() == currentFilter || k.Name.ToLowerInvariant() == currentFilter).ToList();
                else
                    return _mainWindow.PrototypeDetails.Where(k => k.FullName.ToLowerInvariant().Contains(currentFilter)).ToList();
            }
        }
    }
}
