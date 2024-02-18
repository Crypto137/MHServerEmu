using System.Collections;
using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Properties
{
    /// <summary>
    /// A collection of key/value pairs of <see cref="PropertyId"/> and <see cref="PropertyValue"/> sorted by key.
    /// </summary>
    public class PropertyCollection : IEnumerable<KeyValuePair<PropertyId, PropertyValue>>
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        // TODO: reimplement PropertyList data structure from the client?
        protected SortedDictionary<PropertyId, PropertyValue> _propertyList = new();

        public PropertyCollection() { }

        public PropertyValue GetProperty(PropertyId propertyId)
        {
            if (_propertyList.TryGetValue(propertyId, out var value) == false)
                return Logger.WarnReturn(new PropertyValue(), $"Failed to get property value for id {propertyId}");

            return value;
        }

        public void SetProperty(PropertyValue value, PropertyId propertyId) => SetPropertyValue(propertyId, value);
        public void SetProperty(bool value, PropertyId propertyId) => SetPropertyValue(propertyId, Property.ToValue(value));
        public void SetProperty(float value, PropertyId propertyId) => SetPropertyValue(propertyId, Property.ToValue(value));
        public void SetProperty(int value, PropertyId propertyId) => SetPropertyValue(propertyId, Property.ToValue(value));
        public void SetProperty(long value, PropertyId propertyId) => SetPropertyValue(propertyId, Property.ToValue(value));
        public void SetProperty(uint value, PropertyId propertyId) => SetPropertyValue(propertyId, Property.ToValue(value));
        public void SetProperty(ulong value, PropertyId propertyId) => SetPropertyValue(propertyId, Property.ToValue(value));
        public void SetProperty(PrototypeId value, PropertyId propertyId) => SetPropertyValue(propertyId, Property.ToValue(value));
        public void SetProperty(CurveId value, PropertyId propertyId) => SetPropertyValue(propertyId, Property.ToValue(value));
        public void SetProperty(AssetId value, PropertyId propertyId) => SetPropertyValue(propertyId, Property.ToValue(value));
        public void SetProperty(Vector3 value, PropertyId propertyId) => SetPropertyValue(propertyId, Property.ToValue(value));

        public bool HasProperty(PropertyEnum propertyEnum)
        {
            foreach (var kvp in this)
            {
                if (kvp.Key.Enum == propertyEnum)
                    return true;
            }

            return false;
        }

        public bool HasProperty(PropertyId propertyId)
        {
            return _propertyList.TryGetValue(propertyId, out _);
        }

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


        #region IEnumerable Implementation

        public IEnumerator<KeyValuePair<PropertyId, PropertyValue>> GetEnumerator() => _propertyList.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        // PropertyCollection::serializeWithDefault

        public virtual void Decode(CodedInputStream stream)
        {
            uint propertyCount = stream.ReadRawUInt32();
            for (int i = 0; i < propertyCount; i++)
            {
                PropertyId id = new(stream.ReadRawVarint64().ReverseBytes());   // Id is reversed so that it can be efficiently encoded into varint when all params are 0
                PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(id.Enum);
                PropertyValue value = ConvertBitsToValue(stream.ReadRawVarint64(), info.DataType);
                _propertyList[id] = value;
            }
        }

        public virtual void Encode(CodedOutputStream stream)
        {
            stream.WriteRawUInt32((uint)_propertyList.Count);
            foreach (var kvp in this)
                SerializePropertyForPacking(kvp, stream);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            int count = 0;
            foreach (var kvp in this)
            {
                PropertyId id = kvp.Key;
                PropertyValue value = kvp.Value;
                PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(id.Enum);

                sb.Append($"Property{count}: ");
                sb.AppendLine($"Id: {id}");
                sb.AppendLine($"Enum: {id.Enum}");

                if (kvp.Key.HasParams)
                {
                    sb.Append($"Params:");
                    PropertyParam[] @params = id.GetParams();
                    for (int i = 0; i < @params.Length; i++)
                        sb.Append($" {@params[i]}");
                    sb.AppendLine();
                }

                sb.AppendLine($"Value: {value.Print(info.DataType)} ({info.DataType})");
                count++;
            }
            return sb.ToString();
        }

        protected bool SetPropertyValue(PropertyId propertyId, PropertyValue propertyValue)
        {
            _propertyList[propertyId] = propertyValue;
            return true;
        }

        protected bool SerializePropertyForPacking(KeyValuePair<PropertyId, PropertyValue> kvp, CodedOutputStream stream)
        {
            // TODO: Serialize only properties that are different from the base collection for replication 
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(kvp.Key.Enum);
            ulong valueBits = ConvertValueToBits(kvp.Value, info.DataType);
            stream.WriteRawVarint64(kvp.Key.Raw.ReverseBytes());    // Id is reversed so that it can be efficiently encoded into varint when all params are 0
            stream.WriteRawVarint64(valueBits);
            return true;
        }

        // TODO: make value <-> bits conversion protected once we no longer need it for hacks
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
    }
}
