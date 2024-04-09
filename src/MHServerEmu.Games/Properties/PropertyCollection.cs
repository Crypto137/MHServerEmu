using System.Collections;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Properties
{
    /// <summary>
    /// An aggregatable collection of key/value pairs of <see cref="PropertyId"/> and <see cref="PropertyValue"/>.
    /// </summary>
    public class PropertyCollection : IEnumerable<KeyValuePair<PropertyId, PropertyValue>>, ISerialize
    {
        // TODO: Eval
        // TODO: PropertyChangeWatcher API: AttachWatcher(), RemoveWatcher(), RemoveAllWatchers()
        // TODO: Consider implementing IDisposable for optimization

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly PropertyList _baseList = new();
        private readonly PropertyList _aggregateList = new();
        private readonly Dictionary<PropertyId, CurveProperty> _curveList = new();

        // Parent and child collections
        // NOTE: The client uses a tabletree structure to store these with PropertyCollection as key and an empty struct called EmptyDummyValue as value.
        // I'm not sure what the intention there was, but it makes zero sense for us to do it the same way.
        private readonly HashSet<PropertyCollection> _parentCollections = new();
        private readonly HashSet<PropertyCollection> _childCollections = new();

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
            get => GetProperty(new(propertyEnum, Property.ToParam(param0)));
            set => SetProperty(value, new(propertyEnum, Property.ToParam(param0)));
        }

        public PropertyValue this[PropertyEnum propertyEnum, PrototypeId param0]
        {
            get => GetProperty(new(propertyEnum, Property.ToParam(propertyEnum, 0, param0)));
            set => SetProperty(value, new(propertyEnum, Property.ToParam(propertyEnum, 0, param0)));
        }

        // 2 params

        public PropertyValue this[PropertyEnum propertyEnum, PropertyParam param0, PropertyParam param1]
        {
            get => GetProperty(new(propertyEnum, param0, param1));
            set => SetProperty(value, new(propertyEnum, param0, param1));
        }

        public PropertyValue this[PropertyEnum propertyEnum, int param0, PrototypeId param1]
        {
            get => GetProperty(new(propertyEnum, (PropertyParam)param0, Property.ToParam(propertyEnum, 1, param1)));
            set => SetProperty(value, new(propertyEnum, (PropertyParam)param0, Property.ToParam(propertyEnum, 1, param1)));
        }

        // 3 params

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

        /// <summary>
        /// Removes the <see cref="PropertyValue"/> with the specified <see cref="PropertyId"/>.
        /// </summary>
        public bool RemoveProperty(PropertyId id)
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
            List<PropertyId> toRemoveList = new();
            foreach (var kvp in this)
            {
                if (kvp.Key.Enum == propertyEnum)
                    toRemoveList.Add(kvp.Key);
            }

            if (toRemoveList.Count == 0) return false;

            foreach (var propertyId in toRemoveList)
                RemoveProperty(propertyId);

            return true;
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
            foreach (var kvp in this)
            {
                if (kvp.Key.Enum == propertyEnum)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="PropertyCollection"/> contains any properties with the specified <see cref="PropertyId"/>.
        /// </summary>
        public bool HasProperty(PropertyId id)
        {
            return _aggregateList.GetPropertyValue(id, out _);
        }

        /// <summary>
        /// Called when a property changes its value.
        /// </summary>
        public void OnPropertyChange(PropertyId id, PropertyValue newValue, PropertyValue oldValue, SetPropertyFlags flags)
        {
            // TODO: Implement as an event that entities can register to?
            
            // Update curve properties that rely on this property as an index property
            foreach (var kvp in IterateCurveProperties())
            {
                if (kvp.Value.IndexPropertyId == id)
                    UpdateCurvePropertyValue(kvp.Value, flags, null);
            }

            // TODO: Update evals

            // TODO: Notify watchers
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
            foreach (var kvp in other.IterateCurveProperties())
            {
                PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(kvp.Key.Enum);
                SetCurveProperty(kvp.Value.PropertyId, kvp.Value.CurveId, kvp.Value.IndexPropertyId, info, SetPropertyFlags.None, cleanCopy);
            }

            // Update curve property values if this is a combination of two different collections rather than a clean copy
            if (cleanCopy == false)
            {
                foreach (var kvp in IterateCurveProperties())
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
            foreach (var kvp in childCollection)
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

        public override string ToString() => _aggregateList.ToString();

        #region IEnumerable Implementation

        // This should iterate over aggregated value list rather than base
        public IEnumerator<KeyValuePair<PropertyId, PropertyValue>> GetEnumerator() => _aggregateList.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        public virtual bool Serialize(Archive archive)
        {
            return SerializeWithDefault(archive, null);
        }

        public virtual bool SerializeWithDefault(Archive archive, PropertyCollection defaultCollection)
        {
            bool success = true;

            // TODO: skip properties that match the default collection

            if (archive.IsPacking)
            {
                // NOTE: PropertyCollection::serializeWithDefault() does a weird thing where it manipulates the archive buffer directly.
                // First it allocates 4 bytes for the number of properties, than it writes all the properties, and then it goes back
                // and updates the number. This is most likely a side effect of not all properties being saved to the database in the
                // original implementation.
                archive.WriteUnencodedStream((uint)_baseList.Count);

                foreach (var kvp in _baseList)
                    success &= SerializePropertyForPacking(kvp, archive, defaultCollection);
            }
            else
            {
                uint numProperties = 0;
                success &= archive.ReadUnencodedStream(ref numProperties);

                for (uint i = 0; i < numProperties; i++)
                {
                    PropertyId id = new();
                    success &= Serializer.Transfer(archive, ref id);

                    PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(id.Enum);

                    ulong bits = 0;
                    success &= Serializer.Transfer(archive, ref bits);

                    if (success)
                        SetPropertyValue(id, ConvertBitsToValue(bits, info.DataType));
                }
            }

            return success;
        }

        #region REMOVEME: Old Serialization

        /// <summary>
        /// Decodes <see cref="PropertyCollection"/> data from a <see cref="CodedInputStream"/>.
        /// </summary>
        public virtual void Decode(CodedInputStream stream)
        {
            uint propertyCount = stream.ReadRawUInt32();
            for (int i = 0; i < propertyCount; i++)
            {
                PropertyId id = new(stream.ReadRawVarint64().ReverseBytes());   // Id is reversed so that it can be efficiently encoded into varint when all params are 0
                PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(id.Enum);
                PropertyValue value = ConvertBitsToValue(stream.ReadRawVarint64(), info.DataType);
                SetPropertyValue(id, value, SetPropertyFlags.Flag0);
            }
        }

        /// <summary>
        /// Encodes <see cref="PropertyCollection"/> data to a <see cref="CodedOutputStream"/>.
        /// </summary>
        public virtual void Encode(CodedOutputStream stream)
        {
            stream.WriteRawUInt32((uint)_baseList.Count);
            foreach (var kvp in _baseList)
                OLD_SerializePropertyForPacking(kvp, stream);
        }

        /// <summary>
        /// Serializes a key/value pair of <see cref="PropertyId"/> and <see cref="PropertyValue"/> to a <see cref="CodedOutputStream"/>.
        /// </summary>
        protected static bool OLD_SerializePropertyForPacking(KeyValuePair<PropertyId, PropertyValue> kvp, CodedOutputStream stream)
        {
            // TODO: Serialize only properties that are different from the base collection for replication 
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(kvp.Key.Enum);
            ulong valueBits = ConvertValueToBits(kvp.Value, info.DataType);
            stream.WriteRawVarint64(kvp.Key.Raw.ReverseBytes());
            stream.WriteRawVarint64(valueBits);
            return true;
        }

        #endregion

        /// <summary>
        /// Returns the <see cref="PropertyValue"/> with the specified <see cref="PropertyId"/>.
        /// Falls back to the default value for the property if this <see cref="PropertyCollection"/> does not contain it.
        /// </summary>
        protected PropertyValue GetPropertyValue(PropertyId id)
        {
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(id.Enum);

            // TODO: EvalPropertyValue()

            if (_aggregateList.GetPropertyValue(id, out PropertyValue value) == false)
                return info.DefaultValue;

            return value;
        }

        /// <summary>
        /// Sets the <see cref="PropertyValue"/> for the <see cref="PropertyId"/>.
        /// </summary>
        protected bool SetPropertyValue(PropertyId id, PropertyValue value, SetPropertyFlags flags = SetPropertyFlags.None)
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

            return hasChanged || flags.HasFlag(SetPropertyFlags.Flag2);  // Some kind of flag that forces property value update
        }

        protected static bool SerializePropertyForPacking(KeyValuePair<PropertyId, PropertyValue> kvp, Archive archive, PropertyCollection defaultCollection)
        {
            bool success = true;

            // TODO: Serialize only properties that are different from the base collection for replication 
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(kvp.Key.Enum);

            ulong id = kvp.Key.Raw.ReverseBytes();  // Id is reversed so that it can be efficiently encoded into varint when all params are 0
            ulong value = ConvertValueToBits(kvp.Value, info.DataType);
            success &= Serializer.Transfer(archive, ref id);
            success &= Serializer.Transfer(archive, ref value);

            return success;
        }

        // TODO: make value <-> bits conversion protected once we no longer need it for hacks

        /// <summary>
        /// Converts a <see cref="PropertyValue"/> to a <see cref="ulong"/> bit representation.
        /// </summary>
        public static ulong ConvertValueToBits(PropertyValue value, PropertyDataType type)
        {
            switch (type)
            {
                case PropertyDataType.Real:
                case PropertyDataType.Curve:        return BitConverter.SingleToUInt32Bits(value.RawFloat);
                case PropertyDataType.Integer:
                case PropertyDataType.Time:         return CodedOutputStream.EncodeZigZag64(value.RawLong);
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
                case PropertyDataType.Curve:        return new(BitConverter.ToSingle(BitConverter.GetBytes(bits)));
                case PropertyDataType.Integer:
                case PropertyDataType.Time:         return new(CodedInputStream.DecodeZigZag64(bits));
                case PropertyDataType.Prototype:    return new(GameDatabase.DataDirectory.GetPrototypeFromEnumValue<Prototype>((int)bits));
                default:                            return new((long)bits);
            }
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
        /// Returns an <see cref="IEnumerable"/> of curve property key/value pairs contained in this <see cref="PropertyCollection"/>.
        /// </summary>
        protected IEnumerable<KeyValuePair<PropertyId, CurveProperty>> IterateCurveProperties()
        {
            foreach (var kvp in _curveList)
                yield return kvp;
        }

        /// <summary>
        /// Updates the <see cref="PropertyValue"/> of the provided <see cref="CurveProperty"/>.
        /// </summary>
        private bool UpdateCurvePropertyValue(CurveProperty curveProp, SetPropertyFlags flags, PropertyInfo info)
        {
            // Retrieve the curve we need
            if (curveProp.CurveId == CurveId.Invalid) Logger.WarnReturn(false, $"UpdateCurvePropertyValue(): curveId is invalid");
            Curve curve = GameDatabase.DataDirectory.CurveDirectory.GetCurve(curveProp.CurveId);

            // Get property info if we didn't get it and make sure it's for a curve property
            if (info == null) info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(curveProp.PropertyId.Enum);
            if (info.IsCurveProperty == false) Logger.WarnReturn(false, $"UpdateCurvePropertyValue(): {curveProp.PropertyId} is not a curve property");

            // Get curve value and round it if needed
            int indexValue = GetPropertyValue(curveProp.IndexPropertyId);
            float resultValue = curve.GetAt(indexValue);
            if (info.TruncatePropertyValueToInt)
                resultValue = MathF.Floor(resultValue);

            // Set the value and aggregate it
            _baseList[curveProp.PropertyId] = resultValue;
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

            foreach (var kvp in childCollection)
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
                    child.GetAggregateValue(id, out PropertyValue childValue);
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
                if (wasAdded || hasChanged || flags.HasFlag(SetPropertyFlags.Flag2))
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
