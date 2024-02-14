using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Properties
{
    /// <summary>
    /// Identifies a <see cref="Property"/>.
    /// </summary>
    public struct PropertyId
    {
        public ulong Raw { get; set; }
        public PropertyEnum Enum { get => (PropertyEnum)(Raw >> PropertyConsts.ParamBitCount); }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> with <see cref="PropertyEnum.Invalid"/> as its value.
        /// </summary>
        public PropertyId()
        {
            Raw = (ulong)PropertyEnum.Invalid << PropertyConsts.ParamBitCount;
        }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> with no params.
        /// </summary>
        public PropertyId(PropertyEnum propertyEnum)
        {
            Raw = (ulong)propertyEnum << PropertyConsts.ParamBitCount;
        }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> with the provided params
        /// </summary>
        public PropertyId(PropertyEnum propertyEnum, int[] @params)
        {
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyEnum);
            Raw = info.EncodeParameters(propertyEnum, @params).Raw;
        }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> with the provided params
        /// </summary>
        public PropertyId(PropertyEnum propertyEnum, int param0)
        {
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyEnum);
            Raw = info.EncodeParameters(propertyEnum, param0).Raw;
        }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> with the provided params
        /// </summary>
        public PropertyId(PropertyEnum propertyEnum, int param0, int param1)
        {
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyEnum);
            Raw = info.EncodeParameters(propertyEnum, param0, param1).Raw;
        }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> with the provided params
        /// </summary>
        public PropertyId(PropertyEnum propertyEnum, int param0, int param1, int param2)
        {
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyEnum);
            Raw = info.EncodeParameters(propertyEnum, param0, param1, param2).Raw;
        }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> with the provided params
        /// </summary>
        public PropertyId(PropertyEnum propertyEnum, int param0, int param1, int param2, int param3)
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

        public override string ToString() => $"0x{Raw:X}";

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="PropertyId"/> has any param values encoded.
        /// </summary>
        public bool HasParams()
        {
            return (Raw & PropertyConsts.ParamMask) != 0;
        }

        /// <summary>
        /// Returns the value of an encoded param.
        /// </summary>
        public int GetParam(int index)
        {
            return GetParams()[index];
        }

        /// <summary>
        /// Decodes and returns encoded param values.
        /// </summary>
        public int[] GetParams()
        {
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(Enum);
            return info.DecodeParameters(this);
        }
    }
}
