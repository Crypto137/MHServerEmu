using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.Properties
{
    [AssetEnum]
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

    [AssetEnum]
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

    [AssetEnum]
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
}