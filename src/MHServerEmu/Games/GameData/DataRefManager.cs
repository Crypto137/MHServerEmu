namespace MHServerEmu.Games.GameData
{
    // Note: in the client DataRef is a container class for ulong-based data ids.
    // We are currently using ulong enums as is. Every time something mentions
    // a DataRef it's actually a ulong id (e.g. PrototypeId).

    // See DataRefTypes.cs for defined id types.

    public class DataRefManager<T> where T: Enum
    {
        private readonly Dictionary<T, string> _referenceDict = new();
        private readonly Dictionary<string, T> _reverseLookupDict;

        public int Count { get => _referenceDict.Count; }

        public DataRefManager(bool useReverseLookupDict)
        {
            // We can't use a dict for reverse lookup for all ref managers because some reference
            // types (e.g. assets) can have duplicate names
            if (useReverseLookupDict) _reverseLookupDict = new();
        }

        public void AddDataRef(T value, string name)
        {
            _referenceDict.Add(value, name);
            if (_reverseLookupDict != null) _reverseLookupDict.Add(name, value);
        }

        public T GetDataRefByName(string name)
        {
            // Try to use a lookup dict first
            if (_reverseLookupDict != null)
            {
                if (_reverseLookupDict.TryGetValue(name, out T dataRef) == false)
                    return default;

                return dataRef;
            }

            // Fall back to linear search if there's no dict
            foreach (var kvp in _referenceDict)
                if (kvp.Value == name) return kvp.Key;

            return default;
        }

        public string GetReferenceName(T dataRef)
        {
            if (_referenceDict.TryGetValue(dataRef, out string name) == false)
                return string.Empty;

            return name;
        }

        public T[] Enumerate()
        {
            T[] refValues = new T[_referenceDict.Count];

            int i = 0;
            foreach (T key in _referenceDict.Keys)
            {
                refValues[i] = key;
                i++;
            }

            Array.Sort(refValues);

            return refValues;
        }
    }
}
