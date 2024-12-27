using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Powers.Conditions;

namespace MHServerEmu.Games.Properties
{
    /// <summary>
    /// Functions for filtering by <see cref="PropertyEnum"/> when iterating a <see cref="PropertyCollection"/>.
    /// </summary>
    public static class PropertyEnumFilter
    {
        public delegate bool Func(PropertyEnum propertyEnum);

        public static Func AggFunc { get; } = Agg;
        public static Func SerializeConditionSrcToConditionFunc { get; } = SerializeConditionSrcToCondition;
        public static Func SerializeEntityToPowerPayloadFunc { get; } = SerializeEntityToPowerPayload;
        public static Func SerializePowerToPowerPayloadFunc { get; } = SerializePowerToPowerPayload;

        /// <summary>
        /// Includes properties that have a valid <see cref="AggregationMethod"/>.
        /// </summary>
        private static bool Agg(PropertyEnum propertyEnum)
        {
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyEnum);
            return info.Prototype.AggMethod != AggregationMethod.None;
        }

        /// <summary>
        /// Includes properties that are valid for <see cref="Condition"/> serialization.
        /// </summary>
        private static bool SerializeConditionSrcToCondition(PropertyEnum propertyEnum)
        {
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyEnum);
            return info.Prototype.SerializeConditionSrcToCondition;
        }

        /// <summary>
        /// Includes properties that are valid for <see cref="Entity"/> -> <see cref="PowerPayload"/> transfer.
        /// </summary>
        private static bool SerializeEntityToPowerPayload(PropertyEnum propertyEnum)
        {
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyEnum);
            return info.Prototype.SerializeEntityToPowerPayload;
        }

        /// <summary>
        /// Includes properties that are valid for <see cref="Power"/> -> <see cref="PowerPayload"/> transfer.
        /// </summary>
        private static bool SerializePowerToPowerPayload(PropertyEnum propertyEnum)
        {
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyEnum);
            return info.Prototype.SerializePowerToPowerPayload;
        }
    }
}
