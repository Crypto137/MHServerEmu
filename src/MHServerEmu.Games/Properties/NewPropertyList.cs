using System.Collections;
using System.Text;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Properties
{
    /// <summary>
    /// A bucketed collection of <see cref="KeyValuePair{TKey, TValue}"/> of <see cref="PropertyId"/> and <see cref="PropertyValue"/>.
    /// </summary>
    public class NewPropertyList : IEnumerable<KeyValuePair<PropertyId, PropertyValue>>
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        // PropertyEnumNode stores either a single non-parameterized property value,
        // or a sorted dictionary of property values sharing the same enum.
        //
        // When a property value is assigned, a node is created for it that either
        // stores the non-parameterized value on its own, or instantiates a new dictionary
        // for the parameterized value.
        //
        // When a parameterized value is added to a node that contains only a non-parameterized
        // value, a dictionary is instantiated and both values are added to it.
        //
        // Doing it this way allows us to avoid heap allocations for enum buckets that contain
        // only a single non-parameterized property, which is a pretty common case.
        //
        // NOTE: This implementation is based on NewPropertyList from the client.
        // PropertyDictionary is called PropertyArray in the original implementation.

        private readonly Dictionary<PropertyEnum, PropertyEnumNode> _nodeDict;
        private int _count = 0;

        /// <summary>
        /// Gets the number of key/value pairs contained in this <see cref="PropertyList"/>.
        /// </summary>
        public int Count { get => _count; }

        /// <summary>
        /// Retrieves the <see cref="PropertyValue"/> with the specified <see cref="PropertyId"/>. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool GetPropertyValue(PropertyId id, out PropertyValue value)
        {
            value = default;

            // If we don't have a node for this enum, it means we don't have this property at all
            if (_nodeDict.TryGetValue(id.Enum, out PropertyEnumNode node) == false)
                return false;

            // If the node has a property dictionary, the value will be stored in it
            if (node.ValueDictionary != null)
                return node.ValueDictionary.TryGetValue(id, out value);

            // If the node does not have a property dictionary, it means it contains only a single
            // non-parameterized value.
            if (id.HasParams)
                return false;

            value = node.PropertyValue;
            return true;
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
            oldValue = default;
            PropertyEnum propertyEnum = id.Enum;
            bool isNewNode = false;

            // Created a new node if needed
            if (_nodeDict.TryGetValue(propertyEnum, out PropertyEnumNode node) == false)
            {
                node = new(propertyEnum);
                isNewNode = true;
            }

            // If we do not have an existing dictionary, either update the non-parameterized value,
            // or create a new dictionary to store the parameterized value.
            SortedDictionary<PropertyId, PropertyValue> dict = node.ValueDictionary;
            if (dict == null)
            {
                // Set a non-parameterized value on a node that does not have parameterized values
                if (id.HasParams == false)
                {
                    oldValue = node.PropertyValue;
                    node.PropertyValue = newValue;
                    wasAdded = isNewNode;
                    hasChanged = wasAdded || oldValue.RawLong != newValue.RawLong;

                    if (wasAdded)
                        _count++;

                    _nodeDict[propertyEnum] = node;     // Update the struct stored in the enum dictionary when we change non-parameterized value
                    return;
                }

                // If our id has params, we need to create a dictionary to store it
                dict = new();                           // The client preallocates a size of 3 here
                node.ValueDictionary = dict;
                _nodeDict[propertyEnum] = node;         // Update the struct stored in the enum dictionary when we create a new value dictionary

                // Add our existing non-parameterized value to the new dictionary
                if (isNewNode == false)
                {
                    dict.Add(node.PropertyEnum, node.PropertyValue);
                    node.PropertyValue = default;
                }

                // Add the new value
                dict.Add(id, newValue);
                wasAdded = true;
                hasChanged = true;
                _count++;

                return;
            }

            // Add or update a value in the existing dictionary
            wasAdded = dict.TryGetValue(id, out oldValue) == false;
            hasChanged = wasAdded || oldValue.RawLong != newValue.RawLong;
            dict[id] = newValue;

            if (hasChanged)
                _count++;

            // No need to update the enum dictionary node if we are just changing the contents of the same dictionary
        }

        /// <summary>
        /// Retrieves and removes the <see cref="PropertyValue"/> with the specified <see cref="PropertyId"/>. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool RemoveProperty(PropertyId id, out PropertyValue value)
        {
            value = default;
            PropertyEnum propertyEnum = id.Enum;

            // Nothing to remove if there is no node for this enum
            if (_nodeDict.TryGetValue(propertyEnum, out PropertyEnumNode node) == false)
                return false;

            SortedDictionary<PropertyId, PropertyValue> dict = node.ValueDictionary;
            if (dict == null)
            {
                // This is a node that stores a single non-parameterized value,
                // and our id is parameterized, so the requested id will not be in this list.
                if (id.HasParams)
                    return false;

                // Remove the non-parameterized node
                value = node.PropertyValue;
                dict.Remove(propertyEnum);
                _count--;
                return true;
            }

            // Try to remove the value from our value dictionary
            if (dict.Remove(id, out value) == false)
                return false;

            // We have successfully removed the value
            _count--;

            // TODO: Would it be more efficient to allow GC to clean up empty dictionary nodes, or should we leave them in place?
            //if (dict.Count == 0)
            //    _nodeDict.Remove(propertyEnum);

            return true;
        }

        /// <summary>
        /// Removes the <see cref="PropertyValue"/> with the specified <see cref="PropertyId"/>. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool RemoveProperty(PropertyId id)
        {
            return RemoveProperty(id, out _);
        }

        /// <summary>
        /// Clears all data from this <see cref="PropertyList"/>.
        /// </summary>
        public void Clear()
        {
            _nodeDict.Clear();
            _count = 0;
        }

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

        /// <summary>
        /// Contains either a single non-parameterized property value or a collection of parameterized ones.
        /// </summary>
        private struct PropertyEnumNode
        {
            public PropertyEnum PropertyEnum { get; }
            public PropertyValue PropertyValue { get; set; }
            public SortedDictionary<PropertyId, PropertyValue> ValueDictionary { get; set; }

            // PropertyEnumNode always has a count of at least 1 for the non-parameterized property
            public int Count { get => ValueDictionary != null ? ValueDictionary.Count : 1; }

            public PropertyEnumNode(PropertyEnum propertyEnum)
            {
                PropertyEnum = propertyEnum;
                PropertyValue = default;
                ValueDictionary = null;
            }
        }

        #region Iteration

        // TODO

        public PropertyIterator.Enumerator GetEnumerator()
        {
            return new PropertyIterator().GetEnumerator();
        }

        IEnumerator<KeyValuePair<PropertyId, PropertyValue>> IEnumerable<KeyValuePair<PropertyId, PropertyValue>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct PropertyIterator : IEnumerable<KeyValuePair<PropertyId, PropertyValue>>
        {

            public PropertyIterator()
            {

            }

            public Enumerator GetEnumerator()
            {
                return new();
            }

            IEnumerator<KeyValuePair<PropertyId, PropertyValue>> IEnumerable<KeyValuePair<PropertyId, PropertyValue>>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public struct Enumerator : IEnumerator<KeyValuePair<PropertyId, PropertyValue>>
            {
                public KeyValuePair<PropertyId, PropertyValue> Current { get => throw new NotImplementedException(); }
                object IEnumerator.Current { get => Current; }

                public Enumerator()
                {

                }

                public bool MoveNext()
                {
                    throw new NotImplementedException();
                }

                public void Reset()
                {
                    throw new NotSupportedException();
                }

                public void Dispose()
                {
                }
            }
        }

        #endregion
    }
}
