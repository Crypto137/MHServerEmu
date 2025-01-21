using System.Collections;
using System.Text;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Properties
{
    /// <summary>
    /// A bucketed collection of <see cref="KeyValuePair{TKey, TValue}"/> of <see cref="PropertyId"/> and <see cref="PropertyValue"/>.
    /// </summary>
    public class PropertyList : IEnumerable<KeyValuePair<PropertyId, PropertyValue>>
    {
        // PropertyEnumNode stores either a single non-parameterized property value,
        // or a collection of property id/value pairs sharing the same enum.
        //
        // When a property value is assigned, a node is created for it that either
        // stores the non-parameterized value on its own, or instantiates a new PropertyArray
        // for the parameterized value.
        //
        // When a parameterized value is added to a node that contains only a non-parameterized
        // value, a PropertyArray is instantiated and both values are added to it.
        //
        // Doing it this way allows us to avoid heap allocations for enum buckets that contain
        // only a single non-parameterized property, which is a pretty common case.
        //
        // NOTE: This implementation is based on NewPropertyList from the client.

        private readonly Dictionary<PropertyEnum, PropertyEnumNode> _nodeDict = new();
        private int _count = 0;
        private int _version = 0;

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

            // If the node has a property array, the value will be stored in it
            if (node.PropertyArray != null)
                return node.PropertyArray.Value.TryGetValue(id, out value);

            // If the node does not have a property array, it means it contains only a single non-parameterized value.
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
                node = new();
                isNewNode = true;
            }

            // If we do not have an existing property array, either update the non-parameterized value,
            // or create a new property array to store the parameterized value.
            PropertyArray propertyArray;
            PropertyArray? nullablePropertyArray = node.PropertyArray;

            if (nullablePropertyArray == null)
            {
                // Set a non-parameterized value on a node that does not have parameterized values
                if (id.HasParams == false)
                {
                    oldValue = node.PropertyValue;
                    node.PropertyValue = newValue;
                    wasAdded = isNewNode;
                    hasChanged = wasAdded || oldValue.RawLong != newValue.RawLong;

                    if (wasAdded)
                    {
                        _count++;
                        _version++;
                    }

                    _nodeDict[propertyEnum] = node;     // Update the struct stored in the enum dictionary when we change non-parameterized value
                    return;
                }

                // If our id has params, we need to create a property array to store it
                propertyArray = new(3);      // Initial capacity is the same as the client
                node.PropertyArray = propertyArray;

                // Add our existing non-parameterized value to the new property array
                if (isNewNode == false)
                {
                    propertyArray.Add(propertyEnum, node.PropertyValue);
                    node.PropertyValue = default;
                }

                _nodeDict[propertyEnum] = node;         // Update the struct stored in the enum dictionary when we create a new property array

                // Add the new value
                propertyArray.Add(id, newValue);
                wasAdded = true;
                hasChanged = true;
                _count++;
                _version++;

                return;
            }

            // Add or update a value in the existing property array
            propertyArray = nullablePropertyArray.Value;

            if (propertyArray.TryGetValue(id, out oldValue) == false)
            {
                propertyArray.Add(id, newValue);
                wasAdded = true;
                hasChanged = true;
            }
            else
            {
                wasAdded = false;
                hasChanged = oldValue.RawLong != newValue.RawLong;

                if (hasChanged)
                    propertyArray.Replace(id, newValue);
            }

            if (hasChanged)
            {
                _count++;
                _version++;
            }

            // No need to update the enum dictionary if we are just changing the contents of an existing property array
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

            PropertyArray? propertyArray = node.PropertyArray;
            if (propertyArray == null)
            {
                // This is a node that stores a single non-parameterized value,
                // and our id is parameterized, so the requested id will not be in this list.
                if (id.HasParams)
                    return false;

                // Remove the non-parameterized node
                value = node.PropertyValue;
                _nodeDict.Remove(propertyEnum);
                _count--;
                _version++;
                return true;
            }

            // Try to remove the value from our value dictionary
            if (propertyArray.Value.Remove(id, out value) == false)
                return false;

            // We have successfully removed the value
            _count--;
            _version++;

            // Keep the allocated property array in case this property gets added again (TODO: pooling for property array lists?)
            //if (node.Count == 0)
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
        /// Returns the number of properties stored that use the specified <see cref="PropertyEnum"/>.
        /// </summary>
        public int GetCountForPropertyEnum(PropertyEnum propertyEnum)
        {
            if (_nodeDict.TryGetValue(propertyEnum, out PropertyEnumNode node) == false)
                return 0;

            return node.Count;
        }

        /// <summary>
        /// Clears all data from this <see cref="PropertyList"/>.
        /// </summary>
        public void Clear()
        {
            // Keep the lists we allocated for nodes for later reuse when we clear this property list
            foreach (var kvp in _nodeDict)
            {
                PropertyArray? array = kvp.Value.PropertyArray;

                if (array != null)
                    array.Value.Clear();
                else
                    _nodeDict.Remove(kvp.Key);
            }

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
            public PropertyValue PropertyValue { get; set; }
            public PropertyArray? PropertyArray { get; set; }

            // NOTE: It's safe to use nullable for PropertyArray because it's a thin wrapper around List with no state of its own.

            // PropertyEnumNode always has a count of at least 1 for the non-parameterized property
            public int Count { get => PropertyArray != null ? PropertyArray.Value.Count : 1; }

            public PropertyEnumNode()
            {
                PropertyValue = default;
                PropertyArray = null;
            }
        }

        /// <summary>
        /// A collection of <see cref="KeyValuePair{TKey, TValue}"/> of <see cref="PropertyId"/> and <see cref="PropertyValue"/> optimized for smaller sizes.
        /// </summary>
        private readonly struct PropertyArray
        {
            // The current implementation is a simple wrapper around List, which performs best with collections of < 10 elements.
            private readonly List<PropertyPair> _list;

            /// <summary>
            /// Returns the number of <see cref="PropertyId"/>/<see cref="PropertyValue"/> pairs contained in this <see cref="PropertyArray"/>.
            /// </summary>
            public int Count { get => _list.Count; }

            /// <summary>
            /// Constructs a new <see cref="PropertyArray"/> with the specified initial capacity.
            /// </summary>
            public PropertyArray(int capacity = 0)
            {
                _list = new(capacity);
            }

            public void Clear()
            {
                _list.Clear();
            }

            /// <summary>
            /// Adds a new <see cref="PropertyId"/>/<see cref="PropertyValue"/> pair to this <see cref="PropertyArray"/>.
            /// </summary>
            public void Add(PropertyId id, PropertyValue value)
            {
                /*
                if (Find(id, out _) != -1)
                    throw new($"PropertyId {id} already exists.");
                */

                // We add pairs to the list in sorted order to speed up lookups
                PropertyPair pair = new(id, value);

                // No elements in the list - add as is.
                if (_list.Count == 0)
                {
                    _list.Add(pair);
                    return;
                }

                // Larger than the last element in the list - push back.
                // This should be the case we always run into when copying.
                if (_list[^1] <= pair)
                {
                    _list.Add(pair);
                    return;
                }

                // Smaller than the first element - push front.
                if (_list[0] >= pair)
                {
                    _list.Insert(0, pair);
                    return;
                }

                // Binary search for an index in the middle.
                int index = _list.BinarySearch(pair);
                if (index < 0)
                    index = ~index;

                _list.Insert(index, pair);
            }

            /// <summary>
            /// Replaces the <see cref="PropertyValue"/> for the <see cref="PropertyId"/> contained in this list.
            /// Throws an exception if not found.
            /// </summary>
            public void Replace(PropertyId id, PropertyValue value)
            {
                int index = Find(id, out _);
                if (index == -1)
                    throw new($"PropertyId {id} not found.");

                _list[index] = new(id, value);
            }

            /// <summary>
            /// Removes the pair associated with the specified <see cref="PropertyId"/>.
            /// Returns <see langword="false"/> if no pair with the specified <see cref="PropertyId"/> is found.
            /// </summary>
            public bool Remove(PropertyId id, out PropertyValue value)
            {
                int index = Find(id, out value);
                if (index == -1)
                    return false;

                _list.RemoveAt(index);
                return true;
            }

            /// <summary>
            /// Returns the <see cref="PropertyValue"/> for the specified <see cref="PropertyId"/> contained in this <see cref="PropertyArray"/>.
            /// Returns <see langword="false"/> if no pair with the specified <see cref="PropertyId"/> is found.
            /// </summary>
            public bool TryGetValue(PropertyId id, out PropertyValue value)
            {
                return Find(id, out value) != -1;
            }

            /// <summary>
            /// Find the index of the specified <see cref="PropertyId"/> in this <see cref="PropertyArray"/>.
            /// Returns -1 if not found.
            /// </summary>
            private int Find(PropertyId id, out PropertyValue value)
            {
                // Linear search with early breaks. This should be faster than binary search for our use case (small arrays).
                int count = _list.Count;

                for (int i = 0; i < count; i++)
                {
                    PropertyPair pair = _list[i];

                    // Best case: the first element is the pair we are looking for.
                    if (pair.Id == id)
                    {
                        value = pair.Value;
                        return i;
                    }

                    // Early exit if we are past the point of where our pair should be
                    if (pair.Id.Raw > id.Raw)
                        break;
                }
                
                value = default;
                return -1;
            }

            /// <summary>
            /// Returns a new <see cref="Enumerator"/> for this <see cref="PropertyArray"/>.
            /// </summary>
            public Enumerator GetEnumerator()
            {
                return new(this);
            }

            /// <summary>
            /// Iterates <see cref="PropertyId"/>/<see cref="PropertyValue"/> pairs contained in a <see cref="PropertyArray"/>.
            /// </summary>
            public struct Enumerator : IEnumerator<KeyValuePair<PropertyId, PropertyValue>>
            {
                private readonly List<PropertyPair> _list;
                private int _index;

                public KeyValuePair<PropertyId, PropertyValue> Current { get; private set; }
                object IEnumerator.Current { get => Current; }

                public Enumerator(PropertyArray propertyArray)
                {
                    _list = propertyArray._list;
                    _index = -1;

                    Current = default;
                }

                public bool MoveNext()
                {
                    while (++_index < _list.Count)
                    {
                        Current = _list[_index].ToKeyValuePair();
                        return true;
                    }

                    return false;
                }

                public void Reset()
                {
                    _index = -1;
                }

                public void Dispose()
                {
                }
            }
        }

        private readonly struct PropertyPair : IComparable<PropertyPair>
        {
            public readonly PropertyId Id;
            public readonly PropertyValue Value;

            public PropertyPair(PropertyId id, PropertyValue value)
            {
                Id = id;
                Value = value;
            }

            public KeyValuePair<PropertyId, PropertyValue> ToKeyValuePair()
            {
                return new(Id, Value);
            }

            public int CompareTo(PropertyPair other)
            {
                return Id.CompareTo(other.Id);
            }

            public static bool operator >=(PropertyPair left, PropertyPair right) => left.CompareTo(right) >= 0;
            public static bool operator <=(PropertyPair left, PropertyPair right) => left.CompareTo(right) <= 0;
        }

        #region Iteration

        // NOTE: IteratePropertyRange() are basically factory methods for constructing filtered iterators.

        /// <summary>
        /// Returns the default enumerator for this <see cref="PropertyList"/>.
        /// </summary>=
        public Iterator.Enumerator GetEnumerator()
        {
            return new Iterator(this).GetEnumerator();
        }

        /// <summary>
        /// Returns an <see cref="Iterator"/> for <see cref="PropertyId"/> and <see cref="PropertyValue"/> pairs that use the specified <see cref="PropertyEnum"/>.
        /// </summary>
        public Iterator IteratePropertyRange(PropertyEnum propertyEnum)
        {
            return new(this, propertyEnum);
        }

        /// <summary>
        /// Returns an <see cref="Iterator"/> for <see cref="PropertyId"/> and <see cref="PropertyValue"/> pairs that use the specified <see cref="PropertyEnum"/>
        /// and have the specified <see cref="int"/> value as param0.
        /// </summary>
        public Iterator IteratePropertyRange(PropertyEnum propertyEnum, int param0)
        {
            return new(this, propertyEnum, param0);
        }

        /// <summary>
        /// Returns an <see cref="Iterator"/> for <see cref="PropertyId"/> and <see cref="PropertyValue"/> pairs that use the specified <see cref="PropertyEnum"/>
        /// and have the specified <see cref="PrototypeId"/> as param0.
        /// </summary>
        public Iterator IteratePropertyRange(PropertyEnum propertyEnum, PrototypeId param0)
        {
            return new(this, propertyEnum, param0);
        }

        /// <summary>
        /// Returns an <see cref="Iterator"/> for <see cref="PropertyId"/> and <see cref="PropertyValue"/> pairs that use the specified <see cref="PropertyEnum"/>
        /// and have the specified <see cref="PrototypeId"/> as param0 and param1.
        /// </summary>
        public Iterator IteratePropertyRange(PropertyEnum propertyEnum, PrototypeId param0, PrototypeId param1)
        {
            return new(this, propertyEnum, param0, param1);
        }

        /// <summary>
        /// Returns an <see cref="Iterator"/> for <see cref="PropertyId"/> and <see cref="PropertyValue"/> pairs that use any of the specified <see cref="PropertyEnum"/> values.
        /// </summary>
        public Iterator IteratePropertyRange(PropertyEnum[] enums)
        {
            return new(this, enums);
        }

        /// <summary>
        /// Returns an <see cref="Iterator"/> for <see cref="PropertyId"/> and <see cref="PropertyValue"/> pairs that match the provided <see cref="PropertyEnumFilter"/>.
        /// </summary>
        public Iterator IteratePropertyRange(PropertyEnumFilter.Func filterFunc)
        {
            return new(this, filterFunc);
        }

        IEnumerator<KeyValuePair<PropertyId, PropertyValue>> IEnumerable<KeyValuePair<PropertyId, PropertyValue>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Wrapper for <see cref="Enumerator"/> to allow parameterized foreach iteration.
        /// </summary>
        public readonly struct Iterator : IEnumerable<KeyValuePair<PropertyId, PropertyValue>>
        {
            private readonly PropertyList _propertyList;

            // Filters
            private readonly PropertyId _propertyIdFilter;
            private readonly int _numParams;
            private readonly PropertyEnum[] _propertyEnums;
            private readonly PropertyEnumFilter.Func _filterFunc;

            /// <summary>
            /// Constructs a new <see cref="Enumerator"/> with no filters.
            /// </summary>
            public Iterator(PropertyList propertyList)
            {
                _propertyList = propertyList;

                _propertyIdFilter = PropertyId.Invalid;
                _numParams = 0;
                _propertyEnums = null;
                _filterFunc = null;
            }

            /// <summary>
            /// Constructs a new <see cref="Enumerator"/> with the provided filters.
            /// </summary>
            public Iterator(PropertyList propertyList, PropertyEnum propertyEnum)
            {
                _propertyList = propertyList;

                _propertyIdFilter = propertyEnum;
                _numParams = 0;
                _propertyEnums = null;
                _filterFunc = null;
            }

            /// <summary>
            /// Constructs a new <see cref="Enumerator"/> with the provided filters.
            /// </summary>
            public Iterator(PropertyList propertyList, PropertyEnum propertyEnum, int param0)
            {
                _propertyList = propertyList;

                _propertyIdFilter = new(propertyEnum, (PropertyParam)param0);
                _numParams = 1;
                _propertyEnums = null;
                _filterFunc = null;
            }

            /// <summary>
            /// Constructs a new <see cref="Enumerator"/> with the provided filters.
            /// </summary>
            public Iterator(PropertyList propertyList, PropertyEnum propertyEnum, PrototypeId param0)
            {
                _propertyList = propertyList;

                _propertyIdFilter = new(propertyEnum, param0);
                _numParams = 1;
                _propertyEnums = null;
                _filterFunc = null;
            }

            /// <summary>
            /// Constructs a new <see cref="Enumerator"/> with the provided filters.
            /// </summary>
            public Iterator(PropertyList propertyList, PropertyEnum propertyEnum, PrototypeId param0, PrototypeId param1)
            {
                _propertyList = propertyList;

                _propertyIdFilter = new(propertyEnum, param0, param1);
                _numParams = 2;
                _propertyEnums = null;
                _filterFunc = null;
            }

            /// <summary>
            /// Constructs a new <see cref="Enumerator"/> with the provided filters.
            /// </summary>
            public Iterator(PropertyList propertyList, PropertyEnum[] enums)
            {
                _propertyList = propertyList;

                _propertyIdFilter = PropertyId.Invalid;
                _numParams = 0;
                _propertyEnums = enums;
                _filterFunc = null;
            }

            /// <summary>
            /// Constructs a new <see cref="Enumerator"/> with the provided filters.
            /// </summary>
            public Iterator(PropertyList propertyList, PropertyEnumFilter.Func filterFunc)
            {
                _propertyList = propertyList;

                _propertyIdFilter = PropertyId.Invalid;
                _numParams = 0;
                _propertyEnums = null;
                _filterFunc = filterFunc;
            }

            /// <summary>
            /// Returns a new <see cref="Enumerator"/> with this <see cref="Iterator"/>'s filters.
            /// </summary>
            public Enumerator GetEnumerator()
            {
                return new(_propertyList, _propertyIdFilter, _numParams, _propertyEnums, _filterFunc);
            }

            IEnumerator<KeyValuePair<PropertyId, PropertyValue>> IEnumerable<KeyValuePair<PropertyId, PropertyValue>>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            /// <summary>
            /// An implementation of <see cref="IEnumerator"/> for filtered enumeration of <see cref="PropertyId"/> and <see cref="PropertyValue"/> pairs.
            /// </summary>
            public struct Enumerator : IEnumerator<KeyValuePair<PropertyId, PropertyValue>>
            {
                // The list we are enumerating
                private readonly PropertyList _propertyList;
                private readonly int _version;

                // Filters
                private readonly PropertyId _propertyIdFilter;
                private readonly int _numParams;

                private readonly PropertyEnum[] _propertyEnums;
                private readonly PropertyEnumFilter.Func _propertyEnumFilterFunc;

                // Enumeration state
                private Dictionary<PropertyEnum, PropertyEnumNode>.Enumerator _nodeEnumerator;

                // NOTE: We are using separate bool + enumerator fields instead of a nullable
                // because getting a enumerator via Nullable.Value would return a copy, and
                // the original iterator stored in our field would remain unmodified.
                private bool _hasArrayEnumerator;
                private PropertyArray.Enumerator _arrayEnumerator;

                public KeyValuePair<PropertyId, PropertyValue> Current { get; private set; }
                object IEnumerator.Current { get => Current; }

                /// <summary>
                /// Constructs a new <see cref="Enumerator"/> with the provided filters.
                /// </summary>
                public Enumerator(PropertyList propertyList, PropertyId propertyIdFilter, int numParams,
                    PropertyEnum[] propertyEnums, PropertyEnumFilter.Func propertyEnumFilterFunc)
                {
                    _propertyList = propertyList;
                    _version = propertyList._version;

                    _propertyIdFilter = propertyIdFilter;
                    _numParams = numParams;
                    _propertyEnums = propertyEnums;
                    _propertyEnumFilterFunc = propertyEnumFilterFunc;

                    _nodeEnumerator = propertyList._nodeDict.GetEnumerator();
                    _hasArrayEnumerator = false;
                    _arrayEnumerator = default;

                    Current = default;
                }

                public bool MoveNext()
                {
                    Current = default;

                    // Check for list version changes (TODO: reset enumeration instead of throwing?)
                    if (_propertyList._version != _version)
                        throw new("PropertyList was modified during iteration.");

                    // Continue iterating the current node (if we have one)
                    if (AdvanceToValidProperty())
                        return true;

                    // Move on to the next node (while there are still any left)
                    while (_nodeEnumerator.MoveNext() != false)
                    {
                        var kvp = _nodeEnumerator.Current;
                        PropertyEnum propertyEnum = kvp.Key;

                        // Filter nodes
                        if (ValidatePropertyEnum(propertyEnum) == false)
                            continue;

                        PropertyEnumNode node = kvp.Value;

                        // Special handling for non-parameterized nodes
                        if (node.PropertyArray == null)
                        {
                            // We check only the params here because the enum has already been validated in ValidatePropertyEnum()
                            if (_propertyIdFilter.HasParams)
                                continue;

                            Current = new(propertyEnum, node.PropertyValue);
                            _hasArrayEnumerator = false;
                            _arrayEnumerator = default;
                            return true;
                        }

                        // Begin iterating a new enum node
                        _hasArrayEnumerator = true;
                        _arrayEnumerator = node.PropertyArray.Value.GetEnumerator();
                        if (AdvanceToValidProperty())
                            return true;
                    }

                    // The current node is finished and there are no more nodes
                    return false;
                }

                public void Reset()
                {
                    _nodeEnumerator = _propertyList._nodeDict.GetEnumerator();
                    _hasArrayEnumerator = false;
                    _arrayEnumerator = default;
                }

                public void Dispose()
                {
                }

                /// <summary>
                /// Advances <see cref="Enumerator"/> to the next valid property in the current node.
                /// </summary>
                private bool AdvanceToValidProperty()
                {
                    // No valid enumerator for the current node
                    if (_hasArrayEnumerator == false)
                        return false;

                    // Continue iteration until we find a valid property
                    while (_arrayEnumerator.MoveNext())
                    {
                        var kvp = _arrayEnumerator.Current;
                        if (ValidatePropertyParams(kvp.Key) == false)
                            continue;

                        Current = kvp;
                        return true;
                    }

                    // Current node finished
                    return false;
                }

                /// <summary>
                /// Validates the specified <see cref="PropertyEnum"/> for iteration given this <see cref="Enumerator"/>'s filters.
                /// </summary>
                private readonly bool ValidatePropertyEnum(PropertyEnum propertyEnum)
                {
                    if (_propertyIdFilter != PropertyId.Invalid && _propertyIdFilter.Enum != propertyEnum)
                        return false;

                    if (_propertyEnums != null && _propertyEnums.Contains(propertyEnum) == false)
                        return false;

                    if (_propertyEnumFilterFunc != null && _propertyEnumFilterFunc(propertyEnum) == false)
                        return false;

                    return true;
                }

                /// <summary>
                /// Validates the params in the provided <see cref="PropertyId"/> for iteration given this <see cref="Enumerator"/>'s filters.
                /// </summary>
                private readonly bool ValidatePropertyParams(PropertyId propertyIdToCheck)
                {
                    if (_propertyIdFilter == PropertyId.Invalid)
                        return true;

                    for (int i = 0; i < _numParams; i++)
                    {
                        Property.FromParam(_propertyIdFilter, i, out int filterParam);
                        Property.FromParam(propertyIdToCheck, i, out int paramToCompare);

                        if (filterParam != paramToCompare)
                            return false;
                    }

                    return true;
                }
            }
        }

        #endregion
    }
}
