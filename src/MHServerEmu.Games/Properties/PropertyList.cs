using System.Collections;
using System.Text;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Properties
{
    /// <summary>
    /// A collection of key/value pairs of <see cref="PropertyId"/> and <see cref="PropertyValue"/>.
    /// </summary>
    public class PropertyList : IEnumerable<KeyValuePair<PropertyId, PropertyValue>>
    {
        // When iterating, properties that use the same enum need to be grouped together to reduce the number of PropertyInfo lookups.
        // Our current implementation is a wrapper around the regular unsorted Dictionary that we store key/value pairs in.
        // Doing it this way and sorting only when we need to iterate is overall faster than a SortedDictionary.

        private readonly Dictionary<PropertyId, PropertyValue> _dict = new();

        /// <summary>
        /// Gets the number of key/value pairs contained in this <see cref="PropertyList"/>.
        /// </summary>
        public int Count { get => _dict.Count; }

        // Indexer allows access the underlying dictionary directly, skipping checks and making it faster.
        public PropertyValue this[PropertyId id]
        {
            get => _dict[id];
            set => _dict[id] = value;
        }

        /// <summary>
        /// Retrieves the <see cref="PropertyValue"/> with the specified <see cref="PropertyId"/>. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool GetPropertyValue(PropertyId id, out PropertyValue value)
        {
            return _dict.TryGetValue(id, out value);
        }

        /// <summary>
        /// Sets the <see cref="PropertyValue"/> with the specified <see cref="PropertyId"/> if the provided value is different from what is already stored. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool SetPropertyValue(PropertyId id, PropertyValue value)
        {
            GetSetPropertyValue(id, value, out _, out _, out bool hasChanged);
            return hasChanged;
        }

        /// <summary>
        /// Retrieves the <see cref="PropertyValue"/> with the specified <see cref="PropertyId"/> and sets it if the provided value is different from what is already stored.
        /// </summary>
        public void GetSetPropertyValue(PropertyId id, PropertyValue newValue, out PropertyValue oldValue, out bool wasAdded, out bool hasChanged)
        {
            wasAdded = GetPropertyValue(id, out oldValue) == false;
            hasChanged = wasAdded || oldValue.RawLong != newValue.RawLong;

            if (wasAdded || hasChanged)
                _dict[id] = newValue;
        }

        /// <summary>
        /// Removes the <see cref="PropertyValue"/> with the specified <see cref="PropertyId"/>. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool RemoveProperty(PropertyId id) => _dict.Remove(id);

        /// <summary>
        /// Retrieves and removes the <see cref="PropertyValue"/> with the specified <see cref="PropertyId"/>. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool RemoveProperty(PropertyId id, out PropertyValue value) => _dict.Remove(id, out value);

        /// <summary>
        /// Clears all data from this <see cref="PropertyList"/>.
        /// </summary>
        public void Clear() => _dict.Clear();

        public override string ToString()
        {
            StringBuilder sb = new();

            PropertyEnum previousEnum = PropertyEnum.Invalid;
            PropertyInfo info = null;
            foreach (var kvp in this)
            {
                PropertyId id = kvp.Key;
                PropertyValue value = kvp.Value;
                PropertyEnum propertyEnum = id.Enum;

                if (propertyEnum != previousEnum)
                    info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyEnum);

                sb.AppendLine($"{info.BuildPropertyName(id)}: {value.Print(info.DataType)}");
            }
            return sb.ToString();
        }

        public IEnumerator<KeyValuePair<PropertyId, PropertyValue>> GetEnumerator()
        {
            // Sort by key before iterating
            var list = _dict.ToList().OrderBy(kvp => kvp.Key);
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
