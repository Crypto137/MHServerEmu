namespace MHServerEmu.Games.Properties
{
    /// <summary>
    /// Identifies a <see cref="Property"/>.
    /// </summary>
    public struct PropertyId
    {
        public ulong Raw { get; private set; }
        public PropertyEnum Enum { get => (PropertyEnum)(Raw >> PropertyConsts.ParamBitCount); }

        // TODO: the client constructs property ids in Property::ToPropertyId

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
        public PropertyId(PropertyEnum propertyEnum, int param0)
        {
            Raw = (ulong)propertyEnum << PropertyConsts.ParamBitCount;
            // todo: param encoding
        }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> with the provided params
        /// </summary>
        public PropertyId(PropertyEnum propertyEnum, int param0, int param1)
        {
            Raw = (ulong)propertyEnum << PropertyConsts.ParamBitCount;
            // todo: param encoding
        }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> with the provided params
        /// </summary>
        public PropertyId(PropertyEnum propertyEnum, int param0, int param1, int param2)
        {
            Raw = (ulong)propertyEnum << PropertyConsts.ParamBitCount;
            // todo: param encoding
        }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> with the provided params
        /// </summary>
        public PropertyId(PropertyEnum propertyEnum, int param0, int param1, int param2, int param3)
        {
            Raw = (ulong)propertyEnum << PropertyConsts.ParamBitCount;
            // todo: param encoding
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
            return 0;
            //return GetParams()[index];
        }

        /// <summary>
        /// Decodes and returns encoded param values.
        /// </summary>
        public int[] GetParams()
        {
            // PropertyInfo::decodeParameters()
            return null;
        }
    }
}
