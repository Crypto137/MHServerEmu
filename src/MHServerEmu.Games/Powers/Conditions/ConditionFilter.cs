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

        public static Func IsConditionCancelOnHitFunc { get; } = IsConditionCancelOnHit;
        public static Func IsConditionCancelOnKilledFunc { get; } = IsConditionCancelOnKilled;
        public static Func<PrototypeId> IsConditionWithPrototypeFunc { get; } = IsConditionWithPrototype;
        public static Func<PowerPrototype> IsConditionCancelOnPowerUseFunc { get; } = IsConditionCancelOnPowerUse;
        public static Func<PowerPrototype> IsConditionCancelOnPowerUsePostFunc { get; } = IsConditionCancelOnPowerUsePost;
        public static Func IsConditionCancelOnIntraRegionTeleportFunc { get; } = IsConditionCancelOnIntraRegionTeleport;

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

        private static bool IsConditionCancelOnHit(Condition condition)
        {
            return condition.CancelOnFlags.HasFlag(ConditionCancelOnFlags.OnHit);
        }

        private static bool IsConditionCancelOnKilled(Condition condition)
        {
            return condition.CancelOnFlags.HasFlag(ConditionCancelOnFlags.OnKilled);
        }

        private static bool IsConditionCancelOnPowerUse(Condition condition, PowerPrototype powerProto)
        {
            ConditionPrototype conditionProto = condition.ConditionPrototype;
            if (conditionProto == null)
                return false;

            return condition.CancelOnFlags.HasFlag(ConditionCancelOnFlags.OnPowerUse) &&
                (conditionProto.CancelOnPowerUseKeyword == PrototypeId.Invalid || powerProto.HasKeyword(conditionProto.CancelOnPowerUseKeyword.As<KeywordPrototype>()));
        }

        private static bool IsConditionCancelOnIntraRegionTeleport(Condition condition)
        {
            return condition.CancelOnFlags.HasFlag(ConditionCancelOnFlags.OnIntraRegionTeleport);
        }

        private static bool IsConditionCancelOnPowerUsePost(Condition condition, PowerPrototype powerProto)
        {
            ConditionPrototype conditionProto = condition.ConditionPrototype;
            if (conditionProto == null)
                return false;

            return condition.CancelOnFlags.HasFlag(ConditionCancelOnFlags.OnPowerUsePost) &&
                (conditionProto.CancelOnPowerUseKeyword == PrototypeId.Invalid || powerProto.HasKeyword(conditionProto.CancelOnPowerUseKeyword.As<KeywordPrototype>()));
        }

        private static bool IsConditionWithPrototype(Condition condition, PrototypeId protoRef)
        {
            return condition.ConditionPrototypeRef == protoRef;
        }
    }
}
