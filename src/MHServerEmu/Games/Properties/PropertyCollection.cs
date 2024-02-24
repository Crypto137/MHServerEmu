using System.Collections;
using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Properties
{
    /// <summary>
    /// An aggregatable collection of key/value pairs of <see cref="PropertyId"/> and <see cref="PropertyValue"/>.
    /// </summary>
    public class PropertyCollection : IEnumerable<KeyValuePair<PropertyId, PropertyValue>>
    {
        // TODO: Scope protection: IDisposable that sets flags?
        // TODO: Eval
        // TODO: PropertyChangeWatcher API: AttachWatcher(), RemoveWatcher(), RemoveAllWatchers()
        // TODO: OnPropertyChange - implement as an event that entities can register to?
        // TODO: Consider implementing IDisposable

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly PropertyList _baseList = new();
        private readonly PropertyList _aggregateList = new();
        private readonly Dictionary<PropertyId, CurveProperty> _curveList = new();

        // Parent and child collections
        // NOTE: The client uses a tabletree structure to store those with PropertyCollection as key and an empty struct called EmptyDummyValue as value.
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
        public PropertyValue GetProperty(PropertyId propertyId)
        {
            return GetPropertyValue(propertyId);
        }

        /// <summary>
        /// Sets the <see cref="PropertyValue"/> with the specified <see cref="PropertyId"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="PropertyValue"/> can be implicitly converted to and from <see cref="bool"/>, <see cref="float"/>,
        /// <see cref="int"/>, <see cref="long"/>, <see cref="uint"/>, <see cref="ulong"/>, <see cref="PrototypeId"/>,
        /// <see cref="CurveId"/>, <see cref="AssetId"/>, and <see cref="Vector3"/>.
        /// </remarks>
        public void SetProperty(PropertyValue value, PropertyId propertyId)
        {
            SetPropertyValue(propertyId, value);
        }

        /// <summary>
        /// Sets a <see cref="CurveProperty"/> that derives its value from the specified <see cref="CurveId"/> and index <see cref="PropertyId"/>.
        /// </summary>
        public void SetCurveProperty(PropertyId propertyId, CurveId curveId, PropertyId indexPropertyid, PropertyInfo info, UInt32Flags flags, bool updateValue)
        {
            CurveProperty curveProp = new(propertyId, indexPropertyid, curveId);
            _curveList[propertyId] = curveProp;

            if (updateValue)
                UpdateCurvePropertyValue(curveProp, flags, info);
        }

        /// <summary>
        /// Removes the <see cref="PropertyValue"/> with the specified <see cref="PropertyId"/>.
        /// </summary>
        public bool RemoveProperty(PropertyId propertyId)
        {
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyId.Enum);

            // Remove from curve property list if needed
            if (info.IsCurveProperty) 
                _curveList.Remove(propertyId);

            // Remove from the base list
            if (_baseList.RemoveProperty(propertyId) == false)
                return false;

            // Update aggregate value if successfully removed
            UpdateAggregateValueFromBase(propertyId, info, UInt32Flags.None, false, new());
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
        public bool HasProperty(PropertyId propertyId)
        {
            return _aggregateList.GetPropertyValue(propertyId, out _);
        }

        public void OnPropertyChange(PropertyId propertyId, PropertyValue newValue, PropertyValue oldValue, UInt32Flags flags)
        {
            // TODO: update curves
            // TODO: update evals
            // TODO: notify watchers
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
                SetPropertyValue(kvp.Key, kvp.Value, UInt32Flags.None);

            // Transfer curve properties
            foreach (var kvp in other.IterateCurveProperties())
            {
                PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(kvp.Key.Enum);
                SetCurveProperty(kvp.Value.PropertyId, kvp.Value.CurveId, kvp.Value.IndexPropertyId, info, UInt32Flags.None, cleanCopy);
            }

            // Update curve property values if this is a combination of two different collections rather than a clean copy
            if (cleanCopy == false)
            {
                foreach (var kvp in IterateCurveProperties())
                {
                    PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(kvp.Key.Enum);
                    UpdateCurvePropertyValue(kvp.Value, UInt32Flags.None, info);
                }
            }
        }

        public bool AddChildCollection(PropertyCollection childCollection)
        {
            if (childCollection == null)
                return Logger.WarnReturn(false, "AddChildCollection(): childCollection is null");

            if (childCollection == this)
                return Logger.WarnReturn(false, "AddChildCollection(): Attempted to add itself as a child");

            if (_parentCollections.Contains(childCollection))
                return Logger.WarnReturn(false, "AddChildCollection(): Attempted to add a parent as a child");

            // TODO: Scope protection checks

            bool addedToChildCollection = _childCollections.Add(childCollection);
            bool addedToParentCollection = childCollection._parentCollections.Add(this);

            if (addedToChildCollection == false || addedToParentCollection == false)
            {
                _childCollections.Remove(childCollection);
                childCollection._parentCollections.Remove(this);

                if (addedToChildCollection == false)
                    return Logger.WarnReturn(false, $"AddChildCollection(): Failed to add to parent's child collection");

                if (addedToParentCollection == false)
                    return Logger.WarnReturn(false, $"AddChildCollection(): Failed to add to child's parent collection");
            }

            AggregateChildCollection(childCollection);

            return true;
        }

        public bool RemoveChildCollection(PropertyCollection childCollection)
        {
            if (childCollection == null)
                return Logger.WarnReturn(false, "RemoveChildCollection(): childCollection is null");

            // TODO: Protection checks

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

                UpdateAggregateValue(propertyId, info, UInt32Flags.None);
            }

            return true;
        }

        public bool RemoveFromParent(PropertyCollection parentCollection)
        {
            if (parentCollection == null)
                return Logger.WarnReturn(false, "RemoveFromParent(): parentCollection is null");

            return parentCollection.RemoveChildCollection(this);
        }

        public bool IsChildOf(PropertyCollection parentCollection) => _parentCollections.Contains(parentCollection);

        public bool HasChildCollection(PropertyCollection childCollection) => _childCollections.Contains(childCollection);

        public override string ToString() => _aggregateList.ToString();

        #region IEnumerable Implementation

        // This should iterate over aggregated value list rather than base
        public IEnumerator<KeyValuePair<PropertyId, PropertyValue>> GetEnumerator() => _aggregateList.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        // TODO: PropertyCollection::serializeWithDefault

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
                SetPropertyValue(id, value, UInt32Flags.Flag0);
            }
        }

        /// <summary>
        /// Encodes <see cref="PropertyCollection"/> data to a <see cref="CodedOutputStream"/>.
        /// </summary>
        public virtual void Encode(CodedOutputStream stream)
        {
            stream.WriteRawUInt32((uint)_baseList.Count);
            foreach (var kvp in _baseList)
                SerializePropertyForPacking(kvp, stream);
        }

        /// <summary>
        /// Returns the <see cref="PropertyValue"/> corresponding to the specified <see cref="PropertyId"/>.
        /// Falls back to the default value for the property if this <see cref="PropertyCollection"/> does not contain it.
        /// </summary>
        protected PropertyValue GetPropertyValue(PropertyId propertyId)
        {
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyId.Enum);

            // TODO: EvalPropertyValue()

            if (_aggregateList.GetPropertyValue(propertyId, out PropertyValue value) == false)
                return info.DefaultValue;

            return value;
        }

        /// <summary>
        /// Sets the <see cref="PropertyValue"/> of a <see cref="PropertyId"/>.
        /// </summary>
        protected bool SetPropertyValue(PropertyId propertyId, PropertyValue propertyValue, UInt32Flags flags = UInt32Flags.None)
        {
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyId.Enum);

            if (info.TruncatePropertyValueToInt && info.DataType == PropertyDataType.Real)
                propertyValue = MathF.Floor(propertyValue.RawFloat);

            ClampPropertyValue(info.Prototype, ref propertyValue);

            bool hasChanged;

            // Setting a property to its default value actually removes the value from the list,
            // because the collection automatically falls back to the default value if nothing is stored.
            if (propertyValue.RawLong == info.DefaultValue.RawLong)
            {
                hasChanged = _baseList.RemoveProperty(propertyId);
                if (hasChanged)
                    UpdateAggregateValueFromBase(propertyId, info, flags, false, new());
            }
            else
            {
                hasChanged = _baseList.SetPropertyValue(propertyId, propertyValue);
                if (hasChanged)
                    UpdateAggregateValueFromBase(propertyId, info, flags, true, propertyValue);
            }

            return hasChanged || flags.HasFlag(UInt32Flags.Flag2);  // Some kind of flag that forces property value update
        }

        /// <summary>
        /// Serializes a key/value pair of <see cref="PropertyId"/> and <see cref="PropertyValue"/> to a <see cref="CodedOutputStream"/>.
        /// </summary>
        protected static bool SerializePropertyForPacking(KeyValuePair<PropertyId, PropertyValue> kvp, CodedOutputStream stream)
        {
            // TODO: Serialize only properties that are different from the base collection for replication 
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(kvp.Key.Enum);
            ulong valueBits = ConvertValueToBits(kvp.Value, info.DataType);
            stream.WriteRawVarint64(kvp.Key.Raw.ReverseBytes());    // Id is reversed so that it can be efficiently encoded into varint when all params are 0
            stream.WriteRawVarint64(valueBits);
            return true;
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
        /// Retrieves the <see cref="CurveProperty"/> corresponding to a <see cref="PropertyId"/>. Returns <see langword="null"/> if not found.
        /// </summary>
        protected CurveProperty? GetCurveProperty(PropertyId propertyId)
        {
            if (_curveList.TryGetValue(propertyId, out CurveProperty curveProp) == false)
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
        private bool UpdateCurvePropertyValue(CurveProperty curveProp, UInt32Flags flags, PropertyInfo info)
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

        private void RemoveAllChildren()
        {
            while (_childCollections.Count > 0)
                RemoveChildCollection(_childCollections.First());
        }

        private void RemoveFromAllParents()
        {
            // TODO: Call this during disposal if we implement IDisposable
            while (_parentCollections.Count > 0)
                RemoveChildCollection(this);
        }

        private void RebuildAggregateList()
        {
            // Clean up existring aggregated values
            _aggregateList.Clear();

            foreach (var kvp in _baseList)
            {
                PropertyId propertyId = kvp.Key;
                PropertyValue newValue = kvp.Value;

                PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyId.Enum);
                OnPropertyChange(propertyId, newValue, info.DefaultValue, UInt32Flags.None); 
            }

            // TODO: Scope protection

            foreach (PropertyCollection child in _childCollections)
                AggregateChildCollection(child);                
        }

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

        private bool AggregatePropertyFromChildCollectionAdd(PropertyId propertyId, PropertyInfo info, PropertyValue propertyValue)
        {
            bool valueHasChanged = false;

            if (GetAggregateValue(propertyId, out PropertyValue aggregateValue) == false)
                aggregateValue = info.DefaultValue;

            PropertyValue oldValue = aggregateValue;

            AggregatePropertyValue(info, propertyValue, ref aggregateValue);
            valueHasChanged = _aggregateList.SetPropertyValue(propertyId, aggregateValue);

            if (valueHasChanged)
            {
                // TODO: scope protection

                foreach (PropertyCollection parent in _parentCollections)
                    parent.UpdateAggregateValue(propertyId, info, UInt32Flags.None);

                OnPropertyChange(propertyId, aggregateValue, oldValue, UInt32Flags.None);
            }

            return valueHasChanged;
        }

        private bool GetBaseValue(PropertyId propertyId, out PropertyValue propertyValue)
        {
            return _baseList.GetPropertyValue(propertyId, out propertyValue);
        }

        private bool GetAggregateValue(PropertyId propertyId, out PropertyValue propertyValue)
        {
            return _aggregateList.GetPropertyValue(propertyId, out propertyValue);
        }

        private void UpdateAggregateValue(PropertyId propertyId, PropertyInfo info, UInt32Flags flags)
        {
            bool hasBaseValue = GetBaseValue(propertyId, out PropertyValue baseValue);
            UpdateAggregateValueFromBase(propertyId, info, flags, hasBaseValue, baseValue);
        }

        private void UpdateAggregateValueFromBase(PropertyId propertyId, PropertyInfo info, UInt32Flags flags, bool hasValue, PropertyValue baseValue)
        {
            PropertyValue aggregateValue = baseValue;

            if (info.Prototype.AggMethod != AggregationMethod.None)
            {
                // TODO: scope protection

                // Aggregate values from all children
                foreach (PropertyCollection child in _childCollections)
                {
                    child.GetAggregateValue(propertyId, out PropertyValue childValue);
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
            }

            if (hasValue)
            {
                _aggregateList.GetSetPropertyValue(propertyId, aggregateValue, out PropertyValue oldValue, out bool wasAdded, out bool hasChanged);
                if (wasAdded || hasChanged || flags.HasFlag(UInt32Flags.Flag2))
                {
                    if (wasAdded) oldValue = info.DefaultValue;
                    OnPropertyChange(propertyId, aggregateValue, oldValue, flags);
                }
            }
            else
            {
                if (_aggregateList.RemoveProperty(propertyId, out PropertyValue oldValue))
                    OnPropertyChange(propertyId, info.DefaultValue, oldValue, flags);
            }

            // Update parents
            // TODO: scope protection
            foreach (PropertyCollection parent in _parentCollections)
                parent.UpdateAggregateValue(propertyId, info, flags);
        }

        private bool AggregatePropertyValue(PropertyInfo info, PropertyValue input, ref PropertyValue output)
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
