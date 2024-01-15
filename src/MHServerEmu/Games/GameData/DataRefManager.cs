using System.Xml.Linq;

namespace MHServerEmu.Games.GameData
{
    // Note: in the client DataRef is a container class for ulong data ids.
    // We are currently using ulong values as is. Every time something mentions
    // a DataRef it's actually a ulong id (e.g. prototype id).

    public class DataRefManager
    {
        private readonly Dictionary<ulong, string> _referenceDict = new();
        private readonly Dictionary<string, ulong> _reverseLookupDict;

        public int Count { get => _referenceDict.Count; }

        public DataRefManager(bool useReverseLookupDict)
        {
            // We can't use a dict for reverse lookup for all ref managers because some reference
            // types (e.g. assets) can have duplicate names
            if (useReverseLookupDict) _reverseLookupDict = new();
        }

        public void AddDataRef(ulong value, string name)
        {
            _referenceDict.Add(value, name);
            if (_reverseLookupDict != null) _reverseLookupDict.Add(name.ToLower(), value);
        }

        public ulong GetDataRefByName(string name)
        {
            // Try to use a lookup dict first
            if (_reverseLookupDict != null)
            {
                if (_reverseLookupDict.TryGetValue(name.ToLower(), out ulong id))
                    return id;

                return 0;
            }

            // Fall back to linear search if there's no dict
            foreach (var kvp in _referenceDict)
                if (kvp.Value == name) return kvp.Key;

            return 0;
        }

        public string GetReferenceName(ulong id)
        {
            if (_referenceDict.TryGetValue(id, out string name))
                return name;

            return string.Empty;
        }

        public ulong[] Enumerate()
        {
            ulong[] refValues = new ulong[_referenceDict.Count];

            int i = 0;
            foreach (ulong key in _referenceDict.Keys)
            {
                refValues[i] = key;
                i++;
            }

            Array.Sort(refValues);

            return refValues;
        }

        // temporarily add this here until we figure out a better way to implement lookup

        public List<KeyValuePair<ulong, string>> LookupCostume(string pattern)
        {
            List<KeyValuePair<ulong, string>> matchList = new();

            foreach (var kvp in _referenceDict)
                if (kvp.Value.Contains("Entity/Items/Costumes/Prototypes/") && kvp.Value.ToLower().Contains(pattern))
                    matchList.Add(kvp);

            return matchList;
        }

        public List<KeyValuePair<ulong, string>> LookupRegion(string pattern)
        {
            List<KeyValuePair<ulong, string>> matchList = new();

            foreach (var kvp in _referenceDict)
            {
                if (kvp.Value.Contains("Regions/"))
                {
                    string fileName = Path.GetFileName(kvp.Value);
                    if (fileName.Contains("Region") && Path.GetExtension(fileName) == ".prototype" && fileName.ToLower().Contains(pattern))
                        matchList.Add(kvp);
                }
            }

            return matchList;
        }

        public List<ulong> GetCellRefs(string cellSetPath)
        {
            var cellRefs = new List<ulong>();

            foreach (KeyValuePair<string, ulong> kvp in _reverseLookupDict)
            {
                if (kvp.Key.StartsWith(cellSetPath.ToLower()) && kvp.Key.EndsWith(".cell"))
                {
                    cellRefs.Add(kvp.Value);
                }
            }

            return cellRefs;
        }
    }
}
