namespace MHServerEmu.GameServer.Properties
{
    public enum PropertyType
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

    public enum DatabasePolicy  // Frequent and Infrequent seem to be treated the same by the DBPolicyTable enum
    {
        None,
        Frequent,
        Infrequent
    }

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