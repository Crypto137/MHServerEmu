namespace MHServerEmu.Games.GameData
{
    // Note: in the client DataRef is a container class for ulong-based data ids.
    // We are currently using ulong enums as is. Every time something mentions
    // a DataRef it's actually a ulong id (e.g. PrototypeId).

    // See DataRefTypes.cs for defined id types.

    /// <summary>
    /// Manages <typeparamref name="T"/> data references. A data reference is a pair of a <see cref="string"/> name and a <see cref="ulong"/> hash of it typed as an enum.
    /// </summary>
    public class DataRefManager<T> where T: Enum
    {
        private readonly Dictionary<T, string> _referenceDict = new();
        private readonly Dictionary<string, T> _reverseLookupDict;
        private readonly Dictionary<T, string> _formattedNameDict = new();

        /// <summary>
        /// Creates a new <see cref="DataRefManager{T}"/> instance and sets up a reverse lookup dictionary if needed.
        /// </summary>
        public DataRefManager(bool useReverseLookupDict)
        {
            // We can't use a dict for reverse lookup for all ref managers because some reference
            // types (e.g. assets) can have duplicate names
            if (useReverseLookupDict)
                _reverseLookupDict = new();
        }

        /// <summary>
        /// Adds a new <typeparamref name="T"/> data reference.
        /// </summary>
        public void AddDataRef(T value, string name)
        {
            _referenceDict.Add(value, name);

            // Add reverse lookup if this data ref manager has a reverse dict
            if (_reverseLookupDict != null)
                _reverseLookupDict.Add(name.ToLower(), value);  // Convert to lower case to make reverse lookup case insensitive
        }

        /// <summary>
        /// Returns the first occurrence of a <typeparamref name="T"/> data reference with the specified name. This lookup is case insensitive.
        /// </summary>
        public T GetDataRefByName(string name)
        {
            name = name.ToLower();

            // Try to use a lookup dict first
            if (_reverseLookupDict != null)
            {
                if (_reverseLookupDict.TryGetValue(name, out T dataRef) == false)
                    return default;

                return dataRef;
            }

            // Fall back to linear search if there's no dict
            foreach (var kvp in _referenceDict)
            {
                if (kvp.Value.ToLower() == name)
                    return kvp.Key;
            }

            return default;
        }

        /// <summary>
        /// Returns the name of the specified <typeparamref name="T"/> data reference.
        /// </summary>
        public string GetReferenceName(T dataRef)
        {
            if (_referenceDict.TryGetValue(dataRef, out string name) == false)
                return string.Empty;

            return name;
        }

        public string GetFormattedReferenceName(T dataRef)
        {
            // Cache formatted names to avoid unnecessary string allocations.
            lock (_formattedNameDict)
            {
                if (_formattedNameDict.TryGetValue(dataRef, out string formattedName) == false)
                {
                    string name = GetReferenceName(dataRef);
                    formattedName = Path.GetFileNameWithoutExtension(name);
                    _formattedNameDict.Add(dataRef, formattedName);
                }

                return formattedName;
            }
        }
    }
}
