using System.Collections;
using System.Text;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Properties
{
    /// <summary>
    /// A collection of key/value pairs of <see cref="PropertyId"/> and <see cref="PropertyValue"/>.
    /// </summary>
    public class PropertyList : IEnumerable<KeyValuePair<PropertyId, PropertyValue>>
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        // When iterating, properties that use the same enum need to be grouped together to reduce the number of PropertyInfo lookups.
        // Our current implementation is a wrapper around the regular unsorted Dictionary that we store key/value pairs in.
        // Doing it this way and sorting only when we need to iterate is overall faster than a SortedDictionary.

        private readonly Dictionary<PropertyId, PropertyValue> _dict = new();

        public delegate bool PropertyEnumFilter(PropertyEnum propertyEnum);

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
            return _dict.OrderBy(kvp => kvp.Key).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Returns all <see cref="PropertyId"/> and <see cref="PropertyValue"/> pairs that use the specified <see cref="PropertyEnum"/>.
        /// </summary>
        public IEnumerable<KeyValuePair<PropertyId, PropertyValue>> IteratePropertyRange(PropertyEnum propertyEnum)
        {
            foreach (var kvp in this)
            {
                if (kvp.Key.Enum == propertyEnum)
                    yield return kvp;
            }
        }

        /// <summary>
        /// Returns all <see cref="PropertyId"/> and <see cref="PropertyValue"/> pairs that use any of the specified <see cref="PropertyEnum"/> values.
        /// Count specifies how many <see cref="PropertyEnum"/> elements to get from the provided <see cref="IEnumerable"/>.
        /// </summary>
        /// /// <remarks>
        /// This can be potentially slow because our current implementation does not group key/value pairs by enum, so this is checked
        /// against every key/value pair rather than once per enum.
        /// </remarks>
        public IEnumerable<KeyValuePair<PropertyId, PropertyValue>> IteratePropertyRange(PropertyEnum[] enums)
        {
            // TODO: Confirm if this is working as intended
            foreach (var kvp in this)
            {
                if (enums.Contains(kvp.Key.Enum) == false) continue;
                yield return kvp;
            }
        }

        /// <summary>
        /// Returns all <see cref="PropertyId"/> and <see cref="PropertyValue"/> pairs that use the specified <see cref="PropertyEnum"/>
        /// and have the specified <see cref="int"/> value as param0.
        /// </summary>
        public IEnumerable<KeyValuePair<PropertyId, PropertyValue>> IteratePropertyRange(PropertyEnum propertyEnum, int param0)
        {
            foreach (var kvp in this)
            {
                Property.FromParam(kvp.Key, 0, out int itParam0);

                bool match = true;
                match &= kvp.Key.Enum == propertyEnum;
                match &= itParam0 == param0;

                if (match == false) continue;
                yield return kvp;
            }
        }

        /// <summary>
        /// Returns all <see cref="PropertyId"/> and <see cref="PropertyValue"/> pairs that use the specified <see cref="PropertyEnum"/>
        /// and have the specified <see cref="PrototypeId"/> as param0.
        /// </summary>
        public IEnumerable<KeyValuePair<PropertyId, PropertyValue>> IteratePropertyRange(PropertyEnum propertyEnum, PrototypeId param0)
        {
            foreach (var kvp in this)
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId itParam0);

                bool match = true;
                match &= kvp.Key.Enum == propertyEnum;
                match &= itParam0 == param0;

                if (match == false) continue;
                yield return kvp;
            }
        }

        /// <summary>
        /// Returns all <see cref="PropertyId"/> and <see cref="PropertyValue"/> pairs that use the specified <see cref="PropertyEnum"/>
        /// and have the specified <see cref="PrototypeId"/> as param0 and param1.
        /// </summary>
        public IEnumerable<KeyValuePair<PropertyId, PropertyValue>> IteratePropertyRange(PropertyEnum propertyEnum, PrototypeId param0, PrototypeId param1)
        {
            foreach (var kvp in this)
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId itParam0);
                Property.FromParam(kvp.Key, 1, out PrototypeId itParam1);

                bool match = true;
                match &= kvp.Key.Enum == propertyEnum;
                match &= itParam0 == param0;
                match &= itParam1 == param1;

                if (match == false) continue;
                yield return kvp;
            }
        }

        /// <summary>
        /// Returns all <see cref="PropertyId"/> and <see cref="PropertyValue"/> pairs that match the provided <see cref="PropertyEnumFilter"/>.
        /// </summary>
        /// <remarks>
        /// This can be potentially slow because our current implementation does not group key/value pairs by enum, so this filter is executed
        /// on every key/value pair rather than once per enum.
        /// </remarks>
        public IEnumerable<KeyValuePair<PropertyId, PropertyValue>> IteratePropertyRange(PropertyEnumFilter filter)
        {
            if (filter == null)
            {
                Logger.Warn("IteratePropertyRange(): filter == null");
                yield break;
            }

            foreach (var kvp in this)
            {
                if (filter(kvp.Key.Enum) == false) continue;
                yield return kvp;
            }
        }
    }
}
