using System.Collections;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties.Evals;

namespace MHServerEmu.Games.Properties
{
    /// <summary>
    /// An aggregatable collection of key/value pairs of <see cref="PropertyId"/> and <see cref="PropertyValue"/>.
    /// </summary>
    public class PropertyCollection : IEnumerable<KeyValuePair<PropertyId, PropertyValue>>, ISerialize, IPoolable, IDisposable
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly PropertyList _baseList = new();
        private readonly PropertyList _aggregateList = new();
        private readonly Dictionary<PropertyId, CurveProperty> _curveList = new();

        // Parent and child collections
        // NOTE: The client uses a tabletree structure to store these with PropertyCollection as key and an empty struct called EmptyDummyValue as value.
        // I'm not sure what the intention there was, but it makes zero sense for us to do it the same way.
        private readonly HashSet<PropertyCollection> _parentCollections = new();
        private readonly HashSet<PropertyCollection> _childCollections = new();

        // A collection of registered watchers
        private readonly HashSet<IPropertyChangeWatcher> _watchers = new();

        public bool IsEmpty { get => _baseList.Count == 0 && _aggregateList.Count == 0 && _curveList.Count == 0; }

        #region Value Indexers

        // Add more indexers for specific param type combinations as needed

        public PropertyValue this[PropertyId propertyId]
        {
            get => GetProperty(propertyId);
            set => SetProperty(value, propertyId);
        }

        // 0 params

        public PropertyValue this[PropertyEnum propertyEnum]
        {
            get => GetProperty(new(propertyEnum));
            set => SetProperty(value, new(propertyEnum));
        }

        // 1 param

        public PropertyValue this[PropertyEnum propertyEnum, PropertyParam param0]
        {
            get => GetProperty(new(propertyEnum, param0));
            set => SetProperty(value, new(propertyEnum, param0));
        }

        public PropertyValue this[PropertyEnum propertyEnum, int param0]
        {
            get => GetProperty(new(propertyEnum, (PropertyParam)param0));
            set => SetProperty(value, new(propertyEnum, (PropertyParam)param0));
        }

        public PropertyValue this[PropertyEnum propertyEnum, AssetId param0]
        {
            get => GetProperty(new(propertyEnum, param0));
            set => SetProperty(value, new(propertyEnum, param0));
        }

        public PropertyValue this[PropertyEnum propertyEnum, PrototypeId param0]
        {
            get => GetProperty(new(propertyEnum, param0));
            set => SetProperty(value, new(propertyEnum, param0));
        }

        public PropertyValue this[PropertyEnum propertyEnum, PropertyEnum param0]
        {
            get => GetProperty(new(propertyEnum, param0));
            set => SetProperty(value, new(propertyEnum, param0));
        }

        public PropertyValue this[PropertyEnum propertyEnum, DamageType param0]
        {
            get => GetProperty(new(propertyEnum, param0));
            set => SetProperty(value, new(propertyEnum, param0));
        }

        public PropertyValue this[PropertyEnum propertyEnum, ManaType param0]
        {
            get => GetProperty(new(propertyEnum, param0));
            set => SetProperty(value, new(propertyEnum, param0));
        }

        // 2 params

        public PropertyValue this[PropertyEnum propertyEnum, PropertyParam param0, PropertyParam param1]
        {
            get => GetProperty(new(propertyEnum, param0, param1));
            set => SetProperty(value, new(propertyEnum, param0, param1));
        }

        public PropertyValue this[PropertyEnum propertyEnum, AssetId param0, AssetId param1]
        {
            get => GetProperty(new(propertyEnum, param0, param1));
            set => SetProperty(value, new(propertyEnum, param0, param1));
        }

        public PropertyValue this[PropertyEnum propertyEnum, PrototypeId param0, PrototypeId param1]
        {
            get => GetProperty(new(propertyEnum, param0, param1));
            set => SetProperty(value, new(propertyEnum, param0, param1));
        }

        public PropertyValue this[PropertyEnum propertyEnum, int param0, PrototypeId param1]
        {
            get => GetProperty(new(propertyEnum, param0, param1));
            set => SetProperty(value, new(propertyEnum, param0, param1));
        }

        // 3 params

        public PropertyValue this[PropertyEnum propertyEnum, int param0, int param1, PrototypeId param2]
        {
            get => GetProperty(new(propertyEnum, param0, param1, param2));
            set => SetProperty(value, new(propertyEnum, param0, param1, param2));
        }

        public PropertyValue this[PropertyEnum propertyEnum, PropertyParam param0, PropertyParam param1, PropertyParam param2]
        {
            get => GetProperty(new(propertyEnum, param0, param1, param2));
            set => SetProperty(value, new(propertyEnum, param0, param1, param2));
        }

        // 4 params

        public PropertyValue this[PropertyEnum propertyEnum, PropertyParam param0, PropertyParam param1, PropertyParam param2, PropertyParam param3]
        {
            get => GetProperty(new(propertyEnum, param0, param1, param2, param3));
            set => SetProperty(value, new(propertyEnum, param0, param1, param2, param3));
        }

        #endregion

        public bool IsInPool { get; set; }

        public PropertyCollection() { }

        // NOTE: In the client GetProperty() and SetProperty() handle conversion to and from PropertyValue,
        // but we take care of that with implicit casting defined in PropertyValue.cs, so these methods are
        // largely redundant and are kept to avoid deviating from the client API.

        /// <summary>
        /// Returns the <see cref="PropertyValue"/> with the specified <see cref="PropertyId"/>.
        /// Falls back to the default value for the property if this <see cref="PropertyCollection"/> does not contain it.
        /// </summary>
        /// <remarks>
        /// <see cref="PropertyValue"/> can be implicitly converted to and from <see cref="bool"/>, <see cref="float"/>,
        /// <see cref="int"/>, <see cref="long"/>, <see cref="uint"/>, <see cref="ulong"/>, <see cref="PrototypeId"/>,
        /// <see cref="CurveId"/>, <see cref="AssetId"/>, and <see cref="Vector3"/>.
        /// </remarks>
        public PropertyValue GetProperty(PropertyId id)
        {
            return GetPropertyValue(id);
        }

        public void GetPropertyMinMaxFloat(PropertyId id, out float min, out float max)
        {
            // This is ugly
            PropertyInfoTable propertyInfoTable = GameDatabase.PropertyInfoTable;
            PropertyInfo propertyInfo = propertyInfoTable.LookupPropertyInfo(id.Enum);

            if (propertyInfo.DataType != PropertyDataType.Real)
            {
                min = 0f;
                max = 0f;
                Logger.Warn("GetPropertyMinMaxFloat(): Attempting to lookup min/max float values for a non-float property");
                return;
            }

            switch (id.Enum)
            {
                // Default to prototype data
                default:
                    PropertyInfoPrototype propertyInfoProto = propertyInfo.Prototype;
                    min = propertyInfoProto.Min;
                    max = propertyInfoProto.Max;
                    break;

                // Cap to max values for resources
                case PropertyEnum.Endurance:
                    Property.FromParam(id, 0, out int manaType);
                    min = 0f;
                    max = GetProperty(new(PropertyEnum.EnduranceMax, (PropertyParam)manaType));

                    break;

                case PropertyEnum.SecondaryResource:
                    min = 0f;
                    max = GetProperty(PropertyEnum.SecondaryResourceMax);

                    break;
            }
        }

        /// <summary>
        /// Sets the <see cref="PropertyValue"/> with the specified <see cref="PropertyId"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="PropertyValue"/> can be implicitly converted to and from <see cref="bool"/>, <see cref="float"/>,
        /// <see cref="int"/>, <see cref="long"/>, <see cref="uint"/>, <see cref="ulong"/>, <see cref="PrototypeId"/>,
        /// <see cref="CurveId"/>, <see cref="AssetId"/>, and <see cref="Vector3"/>.
        /// </remarks>
        public void SetProperty(PropertyValue value, PropertyId id)
        {
            SetPropertyValue(id, value);
        }

        /// <summary>
        /// Sets a <see cref="CurveProperty"/> that derives its value from the specified <see cref="CurveId"/> and index <see cref="PropertyId"/>.
        /// </summary>
        public void SetCurveProperty(PropertyId propertyId, CurveId curveId, PropertyId indexPropertyid, PropertyInfo info, SetPropertyFlags flags, bool updateValue)
        {
            CurveProperty curveProp = new(propertyId, indexPropertyid, curveId);
            _curveList[propertyId] = curveProp;

            if (updateValue)
                UpdateCurvePropertyValue(curveProp, flags, info);
        }

        public CurveId GetCurveIdForCurveProperty(PropertyId curvePropertyId)
        {
            PropertyInfo propertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(curvePropertyId.Enum);
            if (propertyInfo.IsCurveProperty == false)
                return Logger.WarnReturn(CurveId.Invalid, $"GetCurveForCurveProperty(): {propertyInfo.PropertyName} is not a curve property");

            if (_curveList.TryGetValue(curvePropertyId, out CurveProperty curveProperty) == false)
                return propertyInfo.DefaultValue;

            return curveProperty.CurveId;
        }

        public PropertyId GetIndexPropertyIdForCurveProperty(PropertyId curvePropertyId)
        {
            PropertyInfo propertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(curvePropertyId.Enum);
            if (propertyInfo.IsCurveProperty == false)
                return Logger.WarnReturn(PropertyId.Invalid, $"GetIndexPropertyIdForCurveProperty(): {propertyInfo.PropertyName} is not a curve property");

            if (_curveList.TryGetValue(curvePropertyId, out CurveProperty curveProperty) == false)
                return PropertyId.Invalid;

            return curveProperty.IndexPropertyId;
        }

        public void GetPropertyCurveIndexPropertyEnumValues(HashSet<PropertyEnum> enumSet)
        {
            foreach (CurveProperty curveProperty in _curveList.Values)
                enumSet.Add(curveProperty.IndexPropertyId.Enum);
        }

        /// <summary>
        /// Adds the specified <see cref="int"/> delta to the <see cref="PropertyValue"/> with the provided <see cref="PropertyId"/>.
        /// </summary>
        public void AdjustProperty(int delta, PropertyId propertyId)
        {
            if (delta == 0) return;

            if (GetBaseValue(propertyId, out PropertyValue value) == false)
                value = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyId.Enum).DefaultValue;

            value += delta;
            SetProperty(value, propertyId);
        }

        /// <summary>
        /// Adds the specified <see cref="float"/> delta to the <see cref="PropertyValue"/> with the provided <see cref="PropertyId"/>.
        /// </summary>
        public void AdjustProperty(float delta, PropertyId propertyId)
        {
            if (Segment.EpsilonTest(delta, 0f)) return;

            if (GetBaseValue(propertyId, out PropertyValue value) == false)
                value = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyId.Enum).DefaultValue;

            value += delta;
            SetProperty(value, propertyId);
        }

        /// <summary>
        /// Removes the <see cref="PropertyValue"/> with the specified <see cref="PropertyId"/>.
        /// </summary>
        public virtual bool RemoveProperty(PropertyId id)
        {
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(id.Enum);

            // Remove from curve property list if needed
            if (info.IsCurveProperty) 
                _curveList.Remove(id);

            // Remove from the base list
            if (_baseList.RemoveProperty(id) == false)
                return false;

            // Update aggregate value if successfully removed
            UpdateAggregateValueFromBase(id, info, SetPropertyFlags.None, false, new());
            return true;
        }

        /// <summary>
        /// Removes all <see cref="PropertyValue"/> values with the specified <see cref="PropertyEnum"/> (no matter what their params are).
        /// </summary>
        public bool RemovePropertyRange(PropertyEnum propertyEnum)
        {
            int propertyEnumCount = _baseList.GetCountForPropertyEnum(propertyEnum);
            if (propertyEnumCount == 0)
                return false;

            Span<PropertyId> range = stackalloc PropertyId[propertyEnumCount];
            int i = 0;
            foreach (var kvp in _baseList.IteratePropertyRange(propertyEnum))
                range[i++] = kvp.Key;

            foreach (PropertyId propertyId in range)
                RemoveProperty(propertyId);

            return true;
        }

        /// <summary>
        /// Copies the <see cref="PropertyValue"/> with the specified <see cref="PropertyId"/> from the provided <see cref="PropertyCollection"/>.
        /// </summary>
        public void CopyProperty(PropertyCollection source, PropertyId id)
        {
            if (source._aggregateList.GetPropertyValue(id, out PropertyValue value))
                SetPropertyValue(id, value);
        }

        /// <summary>
        /// Copies all properties with the specified <see cref="PropertyEnum"/> from the provided <see cref="PropertyCollection"/>.
        /// </summary>
        public void CopyPropertyRange(PropertyCollection source, PropertyEnum propertyEnum)
        {
            foreach (var kvp in source.IteratePropertyRange(propertyEnum))
                SetPropertyValue(kvp.Key, kvp.Value);
        }

        /// <summary>
        /// Returns the number of properties with non-default values in this <see cref="PropertyCollection"/> that use the specified <see cref="PropertyEnum"/>.
        /// </summary>
        public int NumPropertiesInRange(PropertyEnum propertyEnum)
        {
            int numProperties = 0;

            if (propertyEnum != PropertyEnum.Invalid)
            {
                foreach (var kvp in IteratePropertyRange(propertyEnum))
                    numProperties++;
            }

            return numProperties;
        }

        /// <summary>
        /// Clears all data from this <see cref="PropertyCollection"/>.
        /// </summary>
        public void Clear()
        {
            _baseList.Clear();
            _curveList.Clear();
            RemoveAllChildren();
            RebuildAggregateList();
        }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="PropertyCollection"/> contains any properties with the specified <see cref="PropertyEnum"/>.
        /// </summary>
        public bool HasProperty(PropertyEnum propertyEnum)
        {
            PropertyList.Iterator iterator = _aggregateList.IteratePropertyRange(propertyEnum);
            return iterator.GetEnumerator().MoveNext();
        }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="PropertyCollection"/> contains any properties with the specified <see cref="PropertyId"/>.
        /// </summary>
        public bool HasProperty(PropertyId id)
        {
            return _aggregateList.GetPropertyValue(id, out _);
        }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="PropertyCollection"/> contains any properties that are applied over time.
        /// </summary>
        public bool HasOverTimeProperties()
        {
            foreach (var kvp in this)
            {
                if (Property.OverTimeProperties.Contains(kvp.Key.Enum))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Called when a property changes its value.
        /// </summary>
        public void OnPropertyChange(PropertyId id, PropertyValue newValue, PropertyValue oldValue, SetPropertyFlags flags)
        {
            // Update curve properties that rely on this property as an index property
            foreach (var kvp in _curveList)
            {
                if (kvp.Value.IndexPropertyId == id)
                    UpdateCurvePropertyValue(kvp.Value, flags, null);
            }

            // Run eval if needed
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(id.Enum);
            if (info.HasDependentEvals)
            {
                using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                evalContext.Game = Game.Current;
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, this);

                foreach (PropertyId dependentEvalId in info.DependentEvals)
                {
                    PropertyInfo dependentEvalInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(dependentEvalId.Enum);
                    PropertyValue oldDependentValue = GetPropertyValue(dependentEvalId);
                    PropertyValue newDependentValue = EvalPropertyValue(dependentEvalInfo, evalContext);

                    if (newDependentValue.RawLong != oldDependentValue.RawLong)
                    {
                        _aggregateList.SetPropertyValue(dependentEvalId, newDependentValue);
                        OnPropertyChange(dependentEvalId, newDependentValue, oldDependentValue, flags);
                    }
                }
            }

            // Notify watchers
            foreach (IPropertyChangeWatcher watcher in _watchers)
                watcher.OnPropertyChange(id, newValue, oldValue, flags);
        }

        /// <summary>
        /// Copies all data from another <see cref="PropertyCollection"/>.
        /// </summary>
        public void FlattenCopyFrom(PropertyCollection other, bool cleanCopy)
        {
            // Clean up if needed
            if (cleanCopy)
            {
                _baseList.Clear();
                _curveList.Clear();
                RemoveAllChildren();
            }

            // Transfer properties from the other collection
            foreach (var kvp in other)
                SetPropertyValue(kvp.Key, kvp.Value, SetPropertyFlags.None);

            // Transfer curve properties
            foreach (var kvp in other._curveList)
            {
                PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(kvp.Key.Enum);
                SetCurveProperty(kvp.Value.PropertyId, kvp.Value.CurveId, kvp.Value.IndexPropertyId, info, SetPropertyFlags.None, cleanCopy);
            }

            // Update curve property values if this is a combination of two different collections rather than a clean copy
            if (cleanCopy == false)
            {
                foreach (var kvp in _curveList)
                {
                    PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(kvp.Key.Enum);
                    UpdateCurvePropertyValue(kvp.Value, SetPropertyFlags.None, info);
                }
            }
        }

        /// <summary>
        /// Adds a child <see cref="PropertyCollection"/> and aggregates its values.
        /// </summary>
        /// <remarks>
        /// When you add a child collection make sure it's not a parent of parent of this collection, or things are going to break.
        /// </remarks>
        public bool AddChildCollection(PropertyCollection childCollection)
        {
            // Check child collection
            if (childCollection == null)
                return Logger.WarnReturn(false, "AddChildCollection(): childCollection is null");

            if (childCollection == this)
                return Logger.WarnReturn(false, "AddChildCollection(): Attempted to add itself as a child");

            // To make this more safe we might want to add a recursive check of all ancestors here
            if (_parentCollections.Contains(childCollection))
                return Logger.WarnReturn(false, "AddChildCollection(): Attempted to add a parent as a child");

            // Check for protections
            if (IsNotProtected(Protection.Child) == false)
                return Logger.WarnReturn(false, "AddChildCollection(): Property collection protection check failed (parent's child collection)");

            if (childCollection.IsNotProtected(Protection.Parent) == false)
                return Logger.WarnReturn(false, "AddChildCollection(): Property collection protection check failed (child's parent collection)");

            // Try to add the child collection
            bool addedToChildCollection = _childCollections.Add(childCollection);
            bool addedToParentCollection = childCollection._parentCollections.Add(this);

            // Make sure nothing went wrong
            if (addedToChildCollection == false || addedToParentCollection == false)
            {
                _childCollections.Remove(childCollection);
                childCollection._parentCollections.Remove(this);

                if (addedToChildCollection == false)
                    return Logger.WarnReturn(false, $"AddChildCollection(): Failed to add to parent's child collection");

                if (addedToParentCollection == false)
                    return Logger.WarnReturn(false, $"AddChildCollection(): Failed to add to child's parent collection");
            }

            // Aggregate the new child collection
            AggregateChildCollection(childCollection);
            return true;
        }

        /// <summary>
        /// Removes a child <see cref="PropertyCollection"/> and reaggregates values.
        /// </summary>
        public bool RemoveChildCollection(PropertyCollection childCollection)
        {
            if (childCollection == null)
                return Logger.WarnReturn(false, "RemoveChildCollection(): childCollection is null");

            // Check for protections, but continue anyway
            if (IsNotProtected(Protection.Child) == false)
                Logger.Warn("RemoveChildCollection(): Property collection protection check failed (parent's child collection)");

            if (childCollection.IsNotProtected(Protection.Parent) == false)
                Logger.Warn("RemoveChildCollection(): Property collection protection check failed (child's parent collection)");

            // Remove parent / child references
            bool childErasedInParent = _childCollections.Remove(childCollection);
            bool parentErasedInChild = childCollection._parentCollections.Remove(this);

            if (childErasedInParent == false)
                return Logger.WarnReturn(false, "RemoveChildCollection(): Failed to remove from parent's child collection");

            if (parentErasedInChild == false)
                return Logger.WarnReturn(false, "RemoveChildCollection(): Failed to remove from child's parent collection");

            // Cache property info lookups for copying multiple properties of the same type in a row
            PropertyEnum previousEnum = PropertyEnum.Invalid;
            PropertyInfo info = null;
            foreach (var kvp in childCollection.IteratePropertyRange(PropertyEnumFilter.AggFunc))
            {
                PropertyId propertyId = kvp.Key;
                PropertyEnum propertyEnum = propertyId.Enum;
                if (propertyEnum != previousEnum)
                {
                    info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyEnum);
                    previousEnum = propertyEnum;
                }

                UpdateAggregateValue(propertyId, info, SetPropertyFlags.None);
            }

            return true;
        }

        /// <summary>
        /// Removes this <see cref="PropertyCollection"/> from a parent collection.
        /// </summary>
        public bool RemoveFromParent(PropertyCollection parentCollection)
        {
            if (parentCollection == null)
                return Logger.WarnReturn(false, "RemoveFromParent(): parentCollection is null");

            return parentCollection.RemoveChildCollection(this);
        }

        /// <summary>
        /// Checks if this <see cref="PropertyCollection"/> is a child of the provided collection.
        /// </summary>
        public bool IsChildOf(PropertyCollection parentCollection) => _parentCollections.Contains(parentCollection);

        /// <summary>
        /// Checks if this <see cref="PropertyCollection"/> is a parent of the provided collection.
        /// </summary>
        public bool HasChildCollection(PropertyCollection childCollection) => _childCollections.Contains(childCollection);

        /// <summary>
        /// Subscribes the provided <see cref="IPropertyChangeWatcher"/> for property changes happening in this <see cref="PropertyCollection"/>.
        /// </summary>
        public bool AttachWatcher(IPropertyChangeWatcher watcher)
        {
            // VERIFY: m_isDeallocating == false

            if (_watchers.Add(watcher) == false)
                return Logger.WarnReturn(false, $"AttachWatcher(): Failed to attach property change watcher {watcher}");

            foreach (var kvp in this)
                watcher.OnPropertyChange(kvp.Key, kvp.Value, kvp.Value, SetPropertyFlags.Refresh);

            return true;
        }

        /// <summary>
        /// Unsubscribes the provided <see cref="IPropertyChangeWatcher"/> from property changes happening in this <see cref="PropertyCollection"/>.
        /// </summary>
        public bool DetachWatcher(IPropertyChangeWatcher watcher)
        {
            if (watcher == null)
                return Logger.WarnReturn(false, "DetachWatcher(): watcher == null");

            if (_watchers.Remove(watcher) == false)
                return Logger.WarnReturn(false, $"DetachWatcher(): Failed to detach property change watcher {watcher}");

            watcher.Detach(false);

            return true;
        }

        /// <summary>
        /// Removes all subscribed <see cref="IPropertyChangeWatcher"/> instances.
        /// </summary>
        public void RemoveAllWatchers()
        {
            while (_watchers.Count > 0)
                DetachWatcher(_watchers.First());
        }

        public override string ToString() => _aggregateList.ToString();

        #region Iteration

        // NOTE: In the client this is the functionality of PropertyCollection::ConstIterator and NewPropertyList::ConstIterator
        // TODO: If iteration is too slow, switch PropertyList to Dictionary<PropertyEnum, List<KeyValuePair<PropertyId, PropertyValue>>
        // or group KVPs by property enum in some other way.

        // IEnumerable implementation, iterates over _aggregateList

        public PropertyList.Iterator.Enumerator GetEnumerator()
        {
            return _aggregateList.GetEnumerator();
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
        /// Returns all <see cref="PropertyId"/> and <see cref="PropertyValue"/> pairs that use the specified <see cref="PropertyEnum"/>.
        /// </summary>
        public PropertyList.Iterator IteratePropertyRange(PropertyEnum propertyEnum)
        {
            return _aggregateList.IteratePropertyRange(propertyEnum);
        }

        /// <summary>
        /// Returns all <see cref="PropertyId"/> and <see cref="PropertyValue"/> pairs that use any of the specified <see cref="PropertyEnum"/> values.
        /// Count specifies how many <see cref="PropertyEnum"/> elements to get from the provided <see cref="IEnumerable"/>.
        /// </summary>
        /// /// <remarks>
        /// This can be potentially slow because our current implementation does not group key/value pairs by enum, so this is checked
        /// against every key/value pair rather than once per enum.
        /// </remarks>
        public PropertyList.Iterator IteratePropertyRange(PropertyEnum[] enums)
        {
            return _aggregateList.IteratePropertyRange(enums);
        }

        /// <summary>
        /// Returns all <see cref="PropertyId"/> and <see cref="PropertyValue"/> pairs that use the specified <see cref="PropertyEnum"/>
        /// and have the specified <see cref="int"/> value as param0.
        /// </summary>
        public PropertyList.Iterator IteratePropertyRange(PropertyEnum propertyEnum, int param0)
        {
            return _aggregateList.IteratePropertyRange(propertyEnum, param0);
        }

        /// <summary>
        /// Returns all <see cref="PropertyId"/> and <see cref="PropertyValue"/> pairs that use the specified <see cref="PropertyEnum"/>
        /// and have the specified <see cref="PrototypeId"/> as param0.
        /// </summary>
        public PropertyList.Iterator IteratePropertyRange(PropertyEnum propertyEnum, PrototypeId param0)
        {
            return _aggregateList.IteratePropertyRange(propertyEnum, param0);
        }

        /// <summary>
        /// Returns all <see cref="PropertyId"/> and <see cref="PropertyValue"/> pairs that use the specified <see cref="PropertyEnum"/>
        /// and have the specified <see cref="PrototypeId"/> as param0 and param1.
        /// </summary>
        public PropertyList.Iterator IteratePropertyRange(PropertyEnum propertyEnum, PrototypeId param0, PrototypeId param1)
        {
            return _aggregateList.IteratePropertyRange(propertyEnum, param0, param1);
        }

        /// <summary>
        /// Returns all <see cref="PropertyId"/> and <see cref="PropertyValue"/> pairs that match the provided <see cref="PropertyEnumFilter"/>.
        /// </summary>
        /// <remarks>
        /// This can be potentially slow because our current implementation does not group key/value pairs by enum, so this filter is executed
        /// on every key/value pair rather than once per enum.
        /// </remarks>
        public PropertyList.Iterator IteratePropertyRange(PropertyEnumFilter.Func filterFunc)
        {
            return _aggregateList.IteratePropertyRange(filterFunc);
        }

        #endregion

        public virtual void ResetForPool()
        {
            Clear();
        }

        public virtual void Dispose()
        {
            ObjectPoolManager.Instance.Return(this);
        }

        public virtual bool Serialize(Archive archive)
        {
            return SerializeWithDefault(archive, null);
        }

        public virtual bool SerializeWithDefault(Archive archive, PropertyCollection defaultCollection)
        {
            bool success = true;

            if (archive.IsPacking)
            {
                // NOTE: PropertyCollection::serializeWithDefault() does a weird thing where it manipulates the archive buffer directly.
                // First it allocates 4 bytes for the number of properties, than it writes all the properties, and then it goes back
                // and updates the number.

                // Remember current offset and reserve 4 bytes
                long numPropertiesOffset = archive.CurrentOffset;
                archive.WriteUnencodedStream(0u);

                uint numProperties = 0;

                foreach (var kvp in _baseList)
                    success &= SerializePropertyForPacking(kvp, ref numProperties, archive, defaultCollection);

                // Write the number of serialized properties to the reserved bytes
                archive.WriteUnencodedStream(numProperties, numPropertiesOffset);                    
            }
            else
            {
                SetPropertyFlags flags = SetPropertyFlags.Deserialized;
                if (archive.IsPersistent)
                    flags |= SetPropertyFlags.Persistent;

                uint numProperties = 0;
                success &= archive.ReadUnencodedStream(ref numProperties);

                for (uint i = 0; i < numProperties; i++)
                {
                    PropertyId id = new();
                    PropertyValue value = new();
                    bool isValid = false;

                    if (archive.IsPersistent)
                    {
                        // TODO: Deprecated property handling
                        PropertyStore propertyStore = new();
                        success &= propertyStore.Serialize(ref id, ref value, this, archive);
                        if (success)
                            isValid = success;
                    }
                    else
                    {
                        success &= Serializer.Transfer(archive, ref id);

                        PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(id.Enum);

                        if (archive.IsMigration)
                        {
                            // Migration archives serialize all values as int64
                            // This is also true for replication in older versions of the game (e.g. 1.10)
                            success &= Serializer.Transfer(archive, ref value.RawLong);
                            isValid = true;
                        }
                        else
                        {
                            ulong bits = 0;
                            success &= Serializer.Transfer(archive, ref bits);

                            if (success)
                            {
                                value = ConvertBitsToValue(bits, info.DataType);
                                isValid = true;
                            }
                        }
                    }

                    if (isValid)
                        SetPropertyValue(id, value, flags);
                }
            }

            return success;
        }

        public void GetPropertiesForMigration(List<(ulong, ulong)> propertyList)
        {
            PropertyEnum prevProperty = PropertyEnum.Invalid;
            PropertyInfoPrototype propInfoProto = null;

            // Need to use base list for this so that we don't accidentally migrate properties from attached collections.
            foreach (var kvp in _baseList)
            {
                PropertyEnum propertyEnum = kvp.Key.Enum;
                if (propertyEnum != prevProperty)
                {
                    PropertyInfo propInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyEnum);
                    propInfoProto = propInfo.Prototype;
                    prevProperty = propertyEnum;
                }

                // Migrate properties that are not saved to the database, but are supposed to be replicated for transfer
                if (propInfoProto.ReplicateToDatabase == DatabasePolicy.None && propInfoProto.ReplicateForTransfer)
                    propertyList.Add((kvp.Key.Raw, kvp.Value));
            }
        }

        /// <summary>
        /// Converts a <see cref="PropertyValue"/> to a <see cref="ulong"/> bit representation.
        /// </summary>
        public static ulong ConvertValueToBits(PropertyValue value, PropertyDataType type)
        {
            switch (type)
            {
                case PropertyDataType.Real:
                case PropertyDataType.Curve:        return BitConverter.SingleToUInt32Bits(value.RawFloat);
                case PropertyDataType.Integer:      return MathHelper.SwizzleSignBit(value.RawLong);
                case PropertyDataType.Time:         return MathHelper.SwizzleSignBit(value.RawLong - (long)Game.StartTime.TotalMilliseconds);
                case PropertyDataType.Prototype:    return (ulong)GameDatabase.DataDirectory.GetPrototypeEnumValue<Prototype>((PrototypeId)value.RawLong);
                default:                            return (ulong)value.RawLong;
            }
        }

        /// <summary>
        /// Converts a <see cref="ulong"/> bit representation to a <see cref="PropertyValue"/>.
        /// </summary>
        public static PropertyValue ConvertBitsToValue(ulong bits, PropertyDataType type)
        {
            switch (type)
            {
                case PropertyDataType.Real:
                case PropertyDataType.Curve:        return BitConverter.UInt32BitsToSingle((uint)bits);
                case PropertyDataType.Integer:      return MathHelper.UnswizzleSignBit(bits);
                case PropertyDataType.Time:         return (long)Game.StartTime.TotalMilliseconds + MathHelper.UnswizzleSignBit(bits);
                case PropertyDataType.Prototype:    return GameDatabase.DataDirectory.GetPrototypeFromEnumValue<Prototype>((int)bits);
                default:                            return (long)bits;
            }
        }

        /// <summary>
        /// Evaluates a <see cref="PropertyValue"/> given the provided <see cref="PropertyId"/> and <see cref="EvalContextData"/>.
        /// </summary>
        public static PropertyValue EvalProperty(PropertyId id, EvalContextData contextData)
        {
            // NOTE: This function isn't really needed because we use implicit casting for PropertyValue,
            // but we are keeping it anyway to match the client API.
            return EvalPropertyValue(id, contextData);
        }

        /// <summary>
        /// Returns the <see cref="PropertyValue"/> with the specified <see cref="PropertyId"/>.
        /// Falls back to the default value for the property if this <see cref="PropertyCollection"/> does not contain it.
        /// </summary>
        protected PropertyValue GetPropertyValue(PropertyId id)
        {
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(id.Enum);

            // First try running eval
            if (info.IsEvalProperty && info.IsEvalAlwaysCalculated)
            {
                using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, this);
                evalContext.SetReadOnlyVar_PropertyId(EvalContext.Var1, id);
                return EvalPropertyValue(info, evalContext);
            }

            // Fall back to the default value if no value is specified in the aggregate list
            if (_aggregateList.GetPropertyValue(id, out PropertyValue value) == false)
                return info.DefaultValue;

            // Return the value from the aggregate list
            return value;
        }

        /// <summary>
        /// Sets the <see cref="PropertyValue"/> for the <see cref="PropertyId"/>.
        /// </summary>
        protected virtual bool SetPropertyValue(PropertyId id, PropertyValue value, SetPropertyFlags flags = SetPropertyFlags.None)
        {
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(id.Enum);

            if (info.TruncatePropertyValueToInt && info.DataType == PropertyDataType.Real)
                value = MathF.Floor(value.RawFloat);

            ClampPropertyValue(info.Prototype, ref value);

            bool hasChanged;

            // Setting a property to its default value actually removes the value from the list,
            // because the collection automatically falls back to the default value if nothing is stored.
            if (value.RawLong == info.DefaultValue.RawLong)
            {
                hasChanged = _baseList.RemoveProperty(id);
                if (hasChanged)
                    UpdateAggregateValueFromBase(id, info, flags, false, new());
            }
            else
            {
                hasChanged = _baseList.SetPropertyValue(id, value);
                if (hasChanged)
                    UpdateAggregateValueFromBase(id, info, flags, true, value);
            }

            return hasChanged || flags.HasFlag(SetPropertyFlags.Refresh);  // Some kind of flag that forces property value update
        }

        protected bool SerializePropertyForPacking(KeyValuePair<PropertyId, PropertyValue> kvp, ref uint numProperties, Archive archive, PropertyCollection defaultCollection)
        {
            bool success = true;

            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(kvp.Key.Enum);
            PropertyInfoPrototype infoProto = info.Prototype;

            // Filter out properties based on archive serialization mode
            if (archive.IsPersistent)       
            {
                if (infoProto.ReplicateToDatabase == DatabasePolicy.None || info.IsCurveProperty)
                    return true;
            }
            else if (archive.IsMigration)
            {
                if (infoProto.ReplicateForTransfer == false)
                    return true;
            }
            else if (archive.IsReplication)
            {
                // Skip properties that don't match AOI channels for this archive
                if ((infoProto.RepNetwork & archive.GetReplicationPolicyEnum()) == Network.AOINetworkPolicyValues.AOIChannelNone)
                    return true;

                // Skip properties that have the same value as the provided default collection (if there is one)
                if (defaultCollection != null && defaultCollection.GetBaseValue(kvp.Key, out PropertyValue baseValue) && kvp.Value.RawLong == baseValue.RawLong)
                    return true;
            }

            // Serialize
            PropertyId id = kvp.Key;
            PropertyValue value = kvp.Value;

            if (archive.IsPersistent)
            {
                PropertyStore propertyStore = new();
                success &= propertyStore.Serialize(ref id, ref value, this, archive);

                //Logger.Debug($"SerializePropertyForPacking(): Packed {id} for persistent storage");
            }
            else
            {
                success &= Serializer.Transfer(archive, ref id);

                if (archive.IsMigration)
                {
                    // Migration archives serialize all values as int64
                    // This is also true for replication in older versions of the game (e.g. 1.10)
                    success &= Serializer.Transfer(archive, ref value.RawLong);
                }
                else
                {
                    ulong valueBits = ConvertValueToBits(kvp.Value, info.DataType);
                    success &= Serializer.Transfer(archive, ref valueBits);
                }
            }

            numProperties++;        // Increment the number of properties that will be written when we finish iterating

            return success;
        }

        /// <summary>
        /// Retrieves the <see cref="CurveProperty"/> with the <see cref="PropertyId"/>. Returns <see langword="null"/> if not found.
        /// </summary>
        protected CurveProperty? GetCurveProperty(PropertyId id)
        {
            if (_curveList.TryGetValue(id, out CurveProperty curveProp) == false)
                return null;

            return curveProp;
        }

        /// <summary>
        /// Updates the <see cref="PropertyValue"/> of the provided <see cref="CurveProperty"/>.
        /// </summary>
        private bool UpdateCurvePropertyValue(CurveProperty curveProp, SetPropertyFlags flags, PropertyInfo info)
        {
            // Retrieve the curve we need
            if (curveProp.CurveId == CurveId.Invalid) return Logger.WarnReturn(false, "UpdateCurvePropertyValue(): curveProp.CurveId == CurveId.Invalid");

            Curve curve = GameDatabase.DataDirectory.CurveDirectory.GetCurve(curveProp.CurveId);
            if (curve == null) return Logger.WarnReturn(false, "UpdateCurvePropertyValue(): curve == null");

            // Get property info if we didn't get it and make sure it's for a curve property
            if (info == null)
                info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(curveProp.PropertyId.Enum);

            if (info?.IsCurveProperty != true)
                return Logger.WarnReturn(false, $"UpdateCurvePropertyValue(): {curveProp.PropertyId} is not a curve property");

            // Get curve value and round it if needed
            int indexValue = GetPropertyValue(curveProp.IndexPropertyId);
            float resultValue = curve.GetAt(indexValue);
            if (info.TruncatePropertyValueToInt)
                resultValue = MathF.Floor(resultValue);

            // Set the value and aggregate it
            _baseList.SetPropertyValue(curveProp.PropertyId, resultValue);
            UpdateAggregateValueFromBase(curveProp.PropertyId, info, flags, true, resultValue);

            return true;
        }

        /// <summary>
        /// Clamps a <see cref="PropertyValue"/> if needed.
        /// </summary>
        private static void ClampPropertyValue(PropertyInfoPrototype propertyInfoPrototype, ref PropertyValue propertyValue)
        {
            switch (propertyInfoPrototype.Type)
            {
                case PropertyDataType.Boolean:
                    if (propertyValue.RawLong != 0)
                        propertyValue = 1;
                    break;
                case PropertyDataType.Real:
                    if (propertyInfoPrototype.ShouldClampValue)
                        propertyValue = (float)Math.Clamp(propertyValue.RawFloat, propertyInfoPrototype.Min, propertyInfoPrototype.Max);
                    break;
                case PropertyDataType.Integer:
                    if (propertyInfoPrototype.ShouldClampValue)
                        propertyValue = Math.Clamp(propertyValue.RawLong, (long)propertyInfoPrototype.Min, (long)propertyInfoPrototype.Max);
                    break;
            }
        }

        /// <summary>
        /// Removes all children from this <see cref="PropertyCollection"/>.
        /// </summary>
        private void RemoveAllChildren()
        {
            while (_childCollections.Count > 0)
                RemoveChildCollection(_childCollections.First());
        }

        /// <summary>
        /// Removes all parents from this <see cref="PropertyCollection"/>.
        /// </summary>
        private void RemoveFromAllParents()
        {
            // TODO: Call this during disposal if we implement IDisposable
            while (_parentCollections.Count > 0)
                RemoveChildCollection(this);
        }

        /// <summary>
        /// Reaggregates all properties contained in this <see cref="PropertyCollection"/>.
        /// </summary>
        private void RebuildAggregateList()
        {
            // Clean up existring aggregated values
            _aggregateList.Clear();

            foreach (var kvp in _baseList)
            {
                PropertyId propertyId = kvp.Key;
                PropertyValue newValue = kvp.Value;

                PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyId.Enum);
                OnPropertyChange(propertyId, newValue, info.DefaultValue, SetPropertyFlags.None); 
            }

            // Aggregate all child collections, protect to prevent new child collections from being added during iteration
            Protect(Protection.Child);
            foreach (PropertyCollection child in _childCollections)
                AggregateChildCollection(child);
            ReleaseProtection(Protection.Child);
        }

        /// <summary>
        /// Aggregates all properties from a child <see cref="PropertyCollection"/>.
        /// </summary>
        private void AggregateChildCollection(PropertyCollection childCollection)
        {
            // Cache property info lookups for copying multiple properties of the same type in a row
            PropertyEnum previousEnum = PropertyEnum.Invalid;
            PropertyInfo info = null;

            foreach (var kvp in childCollection.IteratePropertyRange(PropertyEnumFilter.AggFunc))
            {
                PropertyId propertyId = kvp.Key;
                PropertyEnum propertyEnum = propertyId.Enum;
                if (propertyId.Enum != previousEnum)
                {
                    info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyEnum);
                    previousEnum = propertyEnum;
                }

                AggregatePropertyFromChildCollectionAdd(propertyId, info, kvp.Value);
            }
        }

        /// <summary>
        /// Aggregates the specified property from a child <see cref="PropertyCollection"/>.
        /// </summary>
        private bool AggregatePropertyFromChildCollectionAdd(PropertyId id, PropertyInfo info, PropertyValue propertyValue)
        {
            bool valueHasChanged;

            if (GetAggregateValue(id, out PropertyValue aggregateValue) == false)
                aggregateValue = info.DefaultValue;

            PropertyValue oldValue = aggregateValue;

            AggregatePropertyValue(info, propertyValue, ref aggregateValue);
            valueHasChanged = _aggregateList.SetPropertyValue(id, aggregateValue);

            if (valueHasChanged)
            {
                // Update parent aggregate values, protect to prevent new parent collections from being added during iteration
                Protect(Protection.Parent);
                foreach (PropertyCollection parent in _parentCollections)
                    parent.UpdateAggregateValue(id, info, SetPropertyFlags.None);
                ReleaseProtection(Protection.Parent);

                OnPropertyChange(id, aggregateValue, oldValue, SetPropertyFlags.None);
            }

            return valueHasChanged;
        }

        /// <summary>
        /// Returns the base value with the specified <see cref="PropertyId"/>.
        /// </summary>
        private bool GetBaseValue(PropertyId id, out PropertyValue value)
        {
            return _baseList.GetPropertyValue(id, out value);
        }

        /// <summary>
        /// Returns the aggregate value with the specified <see cref="PropertyId"/>.
        /// </summary>
        private bool GetAggregateValue(PropertyId id, out PropertyValue value)
        {
            return _aggregateList.GetPropertyValue(id, out value);
        }

        /// <summary>
        /// Updates the aggregate value with the specified <see cref="PropertyId"/>.
        /// </summary>
        private void UpdateAggregateValue(PropertyId id, PropertyInfo info, SetPropertyFlags flags)
        {
            bool hasBaseValue = GetBaseValue(id, out PropertyValue baseValue);
            UpdateAggregateValueFromBase(id, info, flags, hasBaseValue, baseValue);
        }

        /// <summary>
        /// Updates the aggregate value with the specified <see cref="PropertyId"/>.
        /// </summary>
        private void UpdateAggregateValueFromBase(PropertyId id, PropertyInfo info, SetPropertyFlags flags, bool hasValue, PropertyValue baseValue)
        {
            PropertyValue aggregateValue = baseValue;

            if (info.Prototype.AggMethod != AggregationMethod.None)
            {
                // Aggregate values from all children, protect to prevent new child collections from being added during iteration
                Protect(Protection.Child);
                foreach (PropertyCollection child in _childCollections)
                {
                    if (child.GetAggregateValue(id, out PropertyValue childValue) == false)
                        continue;

                    if (hasValue)
                    {
                        AggregatePropertyValue(info, childValue, ref aggregateValue);
                    }
                    else
                    {
                        aggregateValue = childValue;
                        hasValue = true;
                    }
                }
                ReleaseProtection(Protection.Child);
            }

            if (hasValue)
            {
                _aggregateList.GetSetPropertyValue(id, aggregateValue, out PropertyValue oldValue, out bool wasAdded, out bool hasChanged);
                if (wasAdded || hasChanged || flags.HasFlag(SetPropertyFlags.Refresh))
                {
                    if (wasAdded) oldValue = info.DefaultValue;
                    OnPropertyChange(id, aggregateValue, oldValue, flags);
                }
            }
            else
            {
                if (_aggregateList.RemoveProperty(id, out PropertyValue oldValue))
                    OnPropertyChange(id, info.DefaultValue, oldValue, flags);
            }

            // Update parents, protect to prevent new parents from being added during iteration
            Protect(Protection.Parent);
            foreach (PropertyCollection parent in _parentCollections)
                parent.UpdateAggregateValue(id, info, flags);
            ReleaseProtection(Protection.Parent);
        }

        /// <summary>
        /// Aggregates two values.
        /// </summary>
        private static bool AggregatePropertyValue(PropertyInfo info, PropertyValue input, ref PropertyValue output)
        {
            switch (info.DataType)
            {
                case PropertyDataType.Boolean:
                    switch (info.Prototype.AggMethod)
                    {
                        case AggregationMethod.Min:
                            output = input && output;
                            break;

                        case AggregationMethod.Max:
                            output = input || output;
                            break;

                        case AggregationMethod.Set:
                            output = input;
                            break;

                        case AggregationMethod.None:
                            return Logger.WarnReturn(false, $"Property {info.PropertyName} with no aggregation method is attempting to be aggregated");

                        default:
                            return Logger.WarnReturn(false, $"Property {info.PropertyName} specifies an unsupported aggregation method for its type");
                    }
                    break;

                case PropertyDataType.Real:
                case PropertyDataType.Curve:
                    switch (info.Prototype.AggMethod)
                    {
                        case AggregationMethod.Min:
                            output = MathF.Min(input.RawFloat, output.RawFloat);
                            break;

                        case AggregationMethod.Max:
                            output = MathF.Max(input.RawFloat, output.RawFloat);
                            break;

                        case AggregationMethod.Sum:
                            output = input.RawFloat + output.RawFloat;
                            break;

                        case AggregationMethod.Mul:
                            output = input.RawFloat * output.RawFloat;
                            break;

                        case AggregationMethod.None:
                            return Logger.WarnReturn(false, $"Property {info.PropertyName} with no aggregation method is attempting to be aggregated");

                        case AggregationMethod.Set:
                            return Logger.WarnReturn(false, $"Property {info.PropertyName} - numeric properties should not use the 'set' aggregation method");
                    }
                    break;

                case PropertyDataType.Integer:
                    switch (info.Prototype.AggMethod)
                    {
                        case AggregationMethod.Min:
                            output = Math.Min(input.RawLong, output.RawLong);
                            break;

                        case AggregationMethod.Max:
                            output = Math.Max(input.RawLong, output.RawLong);
                            break;

                        case AggregationMethod.Sum:
                            output = input.RawLong + output.RawLong;
                            break;

                        case AggregationMethod.Mul:
                            output = input.RawLong * output.RawLong;
                            break;

                        case AggregationMethod.None:
                            return Logger.WarnReturn(false, $"Property {info.PropertyName} with no aggregation method is attempting to be aggregated");

                        case AggregationMethod.Set:
                            return Logger.WarnReturn(false, $"Property {info.PropertyName} - numeric properties should not use the 'set' aggregation method");
                    }
                    break;

                default:
                    switch (info.Prototype.AggMethod)
                    {
                        case AggregationMethod.Set:
                            output = input;
                            break;

                        case AggregationMethod.None:
                            return Logger.WarnReturn(false, $"Property {info.PropertyName} with no aggregation method is attempting to be aggregated");

                        default:
                            return Logger.WarnReturn(false, $"Property {info.PropertyName} specifies an unsupported aggregation method for its type");
                    }
                    break;
            }

            ClampPropertyValue(info.Prototype, ref output);
            return true;
        }

        #region Eval Calculation

        /// <summary>
        /// Evaluates a <see cref="PropertyValue"/> given the provided <see cref="PropertyId"/> and <see cref="EvalContextData"/>.
        /// </summary>
        private static PropertyValue EvalPropertyValue(PropertyId id, EvalContextData contextData)
        {
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(id.Enum);
            return EvalPropertyValue(info, contextData);
        }

        /// <summary>
        /// Evaluates a <see cref="PropertyValue"/> given the provided <see cref="PropertyInfo"/> and <see cref="EvalContextData"/>.
        /// </summary>
        private static PropertyValue EvalPropertyValue(PropertyInfo info, EvalContextData contextData)
        {
            if (info.IsEvalProperty == false)
                return Logger.WarnReturn(new PropertyValue(), "EvalPropertyValue(): info.IsEvalProperty == false");

            PropertyValue value;
            switch (info.DataType)
            {
                case PropertyDataType.Boolean:
                    value = Eval.RunBool(info.Eval, contextData);                    
                    break;

                case PropertyDataType.Real:
                    value = Eval.RunFloat(info.Eval, contextData);
                    break;

                case PropertyDataType.Integer:
                    value = Eval.RunLong(info.Eval, contextData);
                    break;

                default:
                    return Logger.WarnReturn(new PropertyValue(), $"EvalPropertyValue(): Unsupported eval property data type {info.DataType}");
            }

            ClampPropertyValue(info.Prototype, ref value);
            return value;
        }

        #endregion

        #region Protection Implementation

        // Protection flags to prevent parent / child collections from being added during iteration.

        // NOTE: What the client does is that it creates a struct that increments a flag variable in its constructor
        // and decrements it in the destructor. So when you leave the scope the destructor gets called and it "releases"
        // the protection. We could potentially implement something similar with IDisposable and using, but that would be
        // pretty hacky, so we are just going to increment and decrement everything manually.

        // NOTE: Do we even need this?

        private enum Protection
        {
            Parent,
            Child,
            NumProtections
        }

        private readonly int[] _protections = new int[(int)Protection.NumProtections];

        private void Protect(Protection protection) => _protections[(int)protection]++;
        private void ReleaseProtection(Protection protection) => _protections[(int)protection]--;
        private bool IsNotProtected(Protection protection) => _protections[(int)protection] == 0;

        #endregion

        /// <summary>
        /// A property that derives its value from a <see cref="Curve"/>.
        /// </summary>
        protected struct CurveProperty
        {
            // A curve property derives its value from a curve using a value of another property as curve index.
            // Example: HealthBaseProp = HealthCurve[CombatLevel].

            public PropertyId PropertyId { get; set; }
            public PropertyId IndexPropertyId { get; set; }
            public CurveId CurveId { get; set; }

            public CurveProperty(PropertyId propertyId, PropertyId indexPropertyId, CurveId curveId)
            {
                PropertyId = propertyId;
                IndexPropertyId = indexPropertyId;
                CurveId = curveId;
            }
        }
    }
}
