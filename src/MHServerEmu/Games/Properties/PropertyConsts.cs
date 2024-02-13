using MHServerEmu.Games.GameData.Calligraphy.Attributes;

namespace MHServerEmu.Games.Properties
{
    [AssetEnum((int)Real)]
    public enum PropertyDataType    // Property/PropertyType.type
    {
        Boolean,
        Real,
        Integer,
        Prototype,
        Curve,
        Asset,
        EntityId,
        Time,
        Guid,
        RegionId,
        Int21Vector3
    }

    [AssetEnum((int)None)]
    public enum DatabasePolicy      // Property/DatabasePolicy.type
    {
        UseParent = -4,
        PerField = -3,
        PropertyCollection = -2,
        Invalid = -1,
        None = 0,
        Frequent = 1,               // Frequent and Infrequent seem to be treated as the same thing
        Infrequent = 1,
        PlayerLargeBlob = 2,
    }

    [AssetEnum((int)None)]
    public enum AggregationMethod
    {
        None,
        Min,
        Max,
        Sum,
        Mul,
        Set
    }

    public enum PropertyParamType
    {
        Invalid = -1,
        Integer = 0,
        Asset = 1,
        Prototype = 2
    }

    /// <summary>
    /// Contains constants used for encoding properties.
    /// </summary>
    public static class PropertyConsts
    {
        public const int MaxParamCount = 4;

        // 11 bits for enum, the rest are params defined by PropertyInfo
        public const int EnumBitCount = 11;
        public const int ParamBitCount = 53;

        public const ulong EnumMax = (1ul << EnumBitCount) - 1;
        public const ulong ParamMax = (1ul << ParamBitCount) - 1;

        public const ulong EnumMask = EnumMax << ParamBitCount;
        public const ulong ParamMask = ParamMax;
    }
}
