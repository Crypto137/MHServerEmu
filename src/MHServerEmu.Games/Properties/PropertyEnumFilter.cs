using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Properties
{
    /// <summary>
    /// Functions for filtering by <see cref="PropertyEnum"/> when iterating a <see cref="PropertyCollection"/>.
    /// </summary>
    public static class PropertyEnumFilter
    {
        public delegate bool Func(PropertyEnum propertyEnum);

        public static bool Agg(PropertyEnum propertyEnum)
        {
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyEnum);
            return info.Prototype.AggMethod != AggregationMethod.None;
        }

        public static bool SerializeConditionSrcToCondition(PropertyEnum propertyEnum)
        {
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyEnum);
            return info.Prototype.SerializeConditionSrcToCondition;
        }

        public static bool SerializeEntityToPowerPayload(PropertyEnum propertyEnum)
        {
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyEnum);
            return info.Prototype.SerializeEntityToPowerPayload;
        }

        public static bool SerializePowerToPowerPayload(PropertyEnum propertyEnum)
        {
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyEnum);
            return info.Prototype.SerializePowerToPowerPayload;
        }
    }
}
