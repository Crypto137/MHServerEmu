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

    // note: enum order for DatabasePolicy and AggregationMethod is TBD
    public enum DatabasePolicy
    {
        None,
        Infrequent,
        Frequent
    }

    public enum AggregationMethod
    {
        Sum,
        Min,
        Max,
        Set,
        Mul
    }
}