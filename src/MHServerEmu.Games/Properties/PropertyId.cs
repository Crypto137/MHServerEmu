using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Powers;

namespace MHServerEmu.Games.Properties
{
    /// <summary>
    /// Identifies a <see cref="PropertyValue"/>.
    /// </summary>
    public struct PropertyId : IComparable<PropertyId>, IEquatable<PropertyId>
    {
        public static readonly PropertyId Invalid = new();

        public ulong Raw { get; set; }

        public PropertyEnum Enum { get => (PropertyEnum)(Raw >> Property.ParamBitCount); }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="PropertyId"/> has any param values encoded.
        /// </summary>
        public bool HasParams { get => (Raw & Property.ParamMask) != 0; }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> with <see cref="PropertyEnum.Invalid"/> as its value.
        /// </summary>
        public PropertyId()
        {
            Raw = (ulong)PropertyEnum.Invalid << Property.ParamBitCount;
        }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> with no params.
        /// </summary>
        public PropertyId(PropertyEnum propertyEnum)
        {
            Raw = (ulong)propertyEnum << Property.ParamBitCount;
        }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> with the provided params
        /// </summary>
        public PropertyId(PropertyEnum propertyEnum, PropertyParam[] @params)
        {
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyEnum);
            Raw = info.EncodeParameters(propertyEnum, @params.AsSpan()).Raw;
        }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> with the provided params
        /// </summary>
        public PropertyId(PropertyEnum propertyEnum, in ReadOnlySpan<PropertyParam> @params)
        {
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyEnum);
            Raw = info.EncodeParameters(propertyEnum, @params).Raw;
        }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> with the provided params
        /// </summary>
        public PropertyId(PropertyEnum propertyEnum, PropertyParam param0)
        {
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyEnum);
            Raw = info.EncodeParameters(propertyEnum, param0).Raw;
        }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> with the provided params
        /// </summary>
        public PropertyId(PropertyEnum propertyEnum, AssetId param0)
        {
            Raw = new PropertyId(propertyEnum, Property.ToParam(param0)).Raw;
        }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> with the provided params
        /// </summary>
        public PropertyId(PropertyEnum propertyEnum, PrototypeId param0)
        {
            Raw = new PropertyId(propertyEnum, Property.ToParam(propertyEnum, 0, param0)).Raw;
        }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> with the provided params
        /// </summary>
        public PropertyId(PropertyEnum propertyEnum, DamageType param0) : this(propertyEnum, (PropertyParam)param0)
        {
        }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> with the provided params
        /// </summary>
        public PropertyId(PropertyEnum propertyEnum, ManaType param0) : this(propertyEnum, (PropertyParam)param0)
        {
        }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> with the provided params
        /// </summary>
        public PropertyId(PropertyEnum propertyEnum, PrototypeId param0, PropertyParam param1)
        {
            Raw = new PropertyId(propertyEnum, Property.ToParam(propertyEnum, 0, param0), param1).Raw;
        }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> with the provided params
        /// </summary>
        public PropertyId(PropertyEnum propertyEnum, PropertyParam param0, PrototypeId param1)
        {
            Raw = new PropertyId(propertyEnum, param0, Property.ToParam(propertyEnum, 1, param1)).Raw;
        }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> with the provided params
        /// </summary>
        public PropertyId(PropertyEnum propertyEnum, AssetId param0, AssetId param1)
        {
            Raw = new PropertyId(propertyEnum, Property.ToParam(param0), Property.ToParam(param1)).Raw;
        }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> with the provided params
        /// </summary>
        public PropertyId(PropertyEnum propertyEnum, PropertyEnum param0)
        {
            // This is for properties that have enums for other properties as their params (example: Requirement)
            PropertyInfo paramInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(param0);
            PrototypeId paramProtoRef = paramInfo.Prototype.DataRef;
            Raw = new PropertyId(propertyEnum, paramProtoRef).Raw;
        }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> with the provided params
        /// </summary>
        public PropertyId(PropertyEnum propertyEnum, PropertyParam param0, PropertyParam param1)
        {
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyEnum);
            Raw = info.EncodeParameters(propertyEnum, param0, param1).Raw;
        }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> with the provided params
        /// </summary>
        public PropertyId(PropertyEnum propertyEnum, PrototypeId param0, PrototypeId param1)
        {
            Raw = new PropertyId(propertyEnum, Property.ToParam(propertyEnum, 0, param0), Property.ToParam(propertyEnum, 1, param1)).Raw;
        }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> with the provided params
        /// </summary>
        public PropertyId(PropertyEnum propertyEnum, PrototypeId param0, int param1)
        {
            Raw = new PropertyId(propertyEnum, Property.ToParam(propertyEnum, 0, param0), (PropertyParam)param1).Raw;
        }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> with the provided params
        /// </summary>
        public PropertyId(PropertyEnum propertyEnum, int param0, PrototypeId param1)
        {
            Raw = new PropertyId(propertyEnum, (PropertyParam)param0, Property.ToParam(propertyEnum, 1, param1)).Raw;
        }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> with the provided params
        /// </summary>
        public PropertyId(PropertyEnum propertyEnum, PrototypeId param0, int param1, int param2)
        {
            Raw = new PropertyId(propertyEnum, Property.ToParam(propertyEnum, 0, param0), (PropertyParam)param1, (PropertyParam)param2).Raw;
        }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> with the provided params
        /// </summary>
        public PropertyId(PropertyEnum propertyEnum, PrototypeId param0, PrototypeId param1, PrototypeId param2)
        {
            Raw = new PropertyId(propertyEnum, Property.ToParam(propertyEnum, 0, param0), Property.ToParam(propertyEnum, 1, param1), Property.ToParam(propertyEnum, 2, param2)).Raw;
        }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> with the provided params
        /// </summary>
        public PropertyId(PropertyEnum propertyEnum, PropertyParam param0, PropertyParam param1, PropertyParam param2)
        {
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyEnum);
            Raw = info.EncodeParameters(propertyEnum, param0, param1, param2).Raw;
        }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> with the provided params
        /// </summary>
        public PropertyId(PropertyEnum propertyEnum, PropertyParam param0, PropertyParam param1, PropertyParam param2, PropertyParam param3)
        {
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyEnum);
            Raw = info.EncodeParameters(propertyEnum, param0, param1, param2, param3).Raw;
        }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> from a raw value.
        /// </summary>
        public PropertyId(ulong raw)
        {
            Raw = raw;
        }

        public static implicit operator PropertyId(PropertyEnum propertyEnum) => new(propertyEnum);

        public int CompareTo(PropertyId other)
        {
            return Raw.CompareTo(other.Raw);
        }

        public override bool Equals(object obj)
        {
            if (obj is not PropertyId other) return false;
            return Raw == other.Raw;
        }

        public bool Equals(PropertyId other) => Raw == other.Raw;

        public static bool operator ==(PropertyId left, PropertyId right) => left.Equals(right);
        public static bool operator !=(PropertyId left, PropertyId right) => left.Equals(right) == false;
        public override int GetHashCode() => Raw.GetHashCode();

        public override string ToString() => GameDatabase.PropertyInfoTable.LookupPropertyInfo(Enum).BuildPropertyName(this);

        /// <summary>
        /// Returns the value of an encoded param.
        /// </summary>
        public PropertyParam GetParam(int index)
        {
            Span<PropertyParam> @params = stackalloc PropertyParam[Property.MaxParamCount];
            GetParams(ref @params);
            return @params[index];
        }

        /// <summary>
        /// Decodes encoded param values to a <see cref="Span{T}"/>.
        /// </summary>
        public void GetParams(ref Span<PropertyParam> @params)
        {
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(Enum);
            info.DecodeParameters(this, ref @params);
        }
    }
}
