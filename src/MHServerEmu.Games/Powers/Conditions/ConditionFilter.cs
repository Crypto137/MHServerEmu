using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Powers.Conditions
{
    /// <summary>
    /// Functions for filtering <see cref="Condition"/> instances.
    /// </summary>
    public static class ConditionFilter
    {
        public delegate bool Func(Condition condition);
        public delegate bool Func<T>(Condition condition, T arg);

        public static Func<PrototypeId> IsConditionOfPowerFunc { get; } = IsConditionOfPower;
        public static Func<KeywordPrototype> IsConditionWithKeywordFunc { get; } = IsConditionWithKeyword;
        public static Func<PropertyEnum> IsConditionWithPropertyOfTypeFunc { get; } = IsConditionWithPropertyOfType;
        public static Func<ConditionType> IsConditionOfTypeFunc { get; } = IsConditionOfType;

        /// <summary>
        /// Returns <see langword="true"/> if the provided <see cref="Condition"/> was created by the specified <see cref="Power"/>.
        /// </summary>
        private static bool IsConditionOfPower(Condition condition, PrototypeId powerProtoRef)
        {
            return condition.CreatorPowerPrototypeRef == powerProtoRef;
        }

        /// <summary>
        /// Returns <see langword="true"/> if the provided <see cref="Condition"/> has the specified keyword.
        /// </summary>
        private static bool IsConditionWithKeyword(Condition condition, KeywordPrototype keywordProto)
        {
            return condition.HasKeyword(keywordProto);
        }

        /// <summary>
        /// Returns <see langword="true"/> if the provided <see cref="Condition"/> has properties with the specified <see cref="PropertyEnum"/>.
        /// </summary>
        private static bool IsConditionWithPropertyOfType(Condition condition, PropertyEnum propertyEnum)
        {
            return condition.Properties.HasProperty(propertyEnum);
        }

        /// <summary>
        /// Returns <see langword="true"/> if the provided <see cref="Condition"/> is of the specified <see cref="ConditionType"/>.
        /// </summary>
        private static bool IsConditionOfType(Condition condition, ConditionType conditionType)
        {
            return condition.ConditionPrototype.ConditionType == conditionType;
        }
    }
}
