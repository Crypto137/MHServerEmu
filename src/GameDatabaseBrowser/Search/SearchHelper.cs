using GameDatabaseBrowser.Models;
using MHServerEmu.Games.GameData;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameDatabaseBrowser.Search
{
    public class SearchHelper
    {
        readonly MainWindow _mainWindow;

        public SearchHelper(MainWindow mainWindow) => _mainWindow = mainWindow;

        /// <summary>
        /// Filter the prototype to display as result
        /// </summary>
        public List<PrototypeDetails> GetPrototypeToDisplay(SearchDetails searchDetails)
        {
            return searchDetails.SearchType switch
            {
                SearchType.ByText => GetPrototypeToDisplayFromTextSearch(searchDetails),
                SearchType.ByPrototypeClass => GetPrototypeToDisplayFromClassSearch(searchDetails),
                SearchType.ByPrototypeBlueprint => GetPrototypeToDisplayFromBlueprintSearch(searchDetails),
                _ => null,
            };
        }

        /// <summary>
        /// Filter the prototype to display as result considering prototype blueprint search
        /// </summary>
        private List<PrototypeDetails> GetPrototypeToDisplayFromBlueprintSearch(SearchDetails searchDetails)
        {
            List<PrototypeDetails> prototypeToDisplay = new();
            if (string.IsNullOrWhiteSpace(searchDetails.TextValue))
                return prototypeToDisplay;

            BlueprintId blueprintId = GameDatabase.BlueprintRefManager.GetDataRefByName(searchDetails.TextValue);

            if (blueprintId == BlueprintId.Invalid)
                return prototypeToDisplay;

            foreach (PrototypeId prototypeId in GameDatabase.DataDirectory.IteratePrototypesInHierarchy(blueprintId, GetPrototypeIterateFlags(searchDetails)))
                prototypeToDisplay.Add(_mainWindow.PrototypeDetails.Where(k => k.FullName == GameDatabase.GetPrototypeName(prototypeId)).First());

            return prototypeToDisplay;
        }

        /// <summary>
        /// Filter the prototype to display as result considering prototype class search
        /// </summary>
        private List<PrototypeDetails> GetPrototypeToDisplayFromClassSearch(SearchDetails searchDetails)
        {
            List<PrototypeDetails> prototypeToDisplay = new();
            if (string.IsNullOrWhiteSpace(searchDetails.TextValue))
                return prototypeToDisplay;

            Type classType = AppDomain.CurrentDomain.GetAssemblies().Where(k => k.FullName.Contains("MHServerEmu"))
                .SelectMany(asm => asm.GetTypes()).FirstOrDefault(type => type.Name == searchDetails.TextValue);

            if (classType == null)
                return prototypeToDisplay;

            foreach (PrototypeId prototypeId in GameDatabase.DataDirectory.IteratePrototypesInHierarchy(classType, GetPrototypeIterateFlags(searchDetails)))
                prototypeToDisplay.Add(_mainWindow.PrototypeDetails.Where(k => k.FullName == GameDatabase.GetPrototypeName(prototypeId)).First());

            return prototypeToDisplay;
        }

        /// <summary>
        /// Prepare for prototype iteration
        /// </summary>
        public PrototypeIterateFlags GetPrototypeIterateFlags(SearchDetails searchDetails)
        {
            PrototypeIterateFlags flags = PrototypeIterateFlags.None;
            if (searchDetails.IncludeAbstractClass == false)
                flags |= PrototypeIterateFlags.NoAbstract;

            if (searchDetails.IncludeNotApprovedClass == false)
                flags |= PrototypeIterateFlags.ApprovedOnly;

            return flags;
        }

        /// <summary>
        /// Filter the prototype to display as result considering text search
        /// </summary>
        private List<PrototypeDetails> GetPrototypeToDisplayFromTextSearch(SearchDetails searchDetails)
        {
            string currentFilter = "";
            if (ulong.TryParse(searchDetails.TextValue, out ulong prototypeId))
                currentFilter = GameDatabase.GetPrototypeName((PrototypeId)prototypeId).ToLowerInvariant();
            else
                currentFilter = searchDetails.TextValue.ToLowerInvariant();

            if (string.IsNullOrEmpty(currentFilter))
                return _mainWindow.PrototypeDetails;

            if (searchDetails.NeedReferences)
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
                if (searchDetails.ExactMatch)
                    return _mainWindow.PrototypeDetails.Where(k => k.FullName.ToLowerInvariant() == currentFilter || k.Name.ToLowerInvariant() == currentFilter).ToList();
                else
                    return _mainWindow.PrototypeDetails.Where(k => k.FullName.ToLowerInvariant().Contains(currentFilter)).ToList();
            }
        }
    }
}
