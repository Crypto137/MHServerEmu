namespace MHServerEmu.Games.Properties
{
    public struct PropertyId
    {
        // 11 bits for enum, the rest are params defined by PropertyInfo
        public const int EnumBitCount = 11;
        public const int ParamBitCount = 53;

        public ulong RawId { get; private set; }
        public PropertyEnum Enum { get => (PropertyEnum)(RawId >> ParamBitCount); }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> from a raw value.
        /// </summary>
        public PropertyId(ulong rawId)
        {
            RawId = rawId;
        }

        /// <summary>
        /// Constructs a <see cref="PropertyId"/> from a <see cref="PropertyEnum"/>.
        /// </summary>
        public PropertyId(PropertyEnum propertyEnum)
        {
            RawId = (ulong)propertyEnum << ParamBitCount;
        }

        public override string ToString() => RawId.ToString();
    }
}
