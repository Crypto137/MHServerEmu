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

    public enum DatabasePolicy  // values are most likely wrong here
    {
        None,
        Infrequent,
        Frequent
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
}