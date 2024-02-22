using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PropertyInfoPrototype : Prototype
    {
        public long Version { get; protected set; }
        public AggregationMethod AggMethod { get; protected set; }             // A Property/AggregationMethod.type
        public double Min { get; protected set; }
        public double Max { get; protected set; }
        public DatabasePolicy ReplicateToDatabase { get; protected set; }      // A Property/DatabasePolicy.type
        public bool ReplicateToProximity { get; protected set; }
        public bool ReplicateToParty { get; protected set; }
        public bool ReplicateToOwner { get; protected set; }
        public bool ReplicateToDiscovery { get; protected set; }
        public bool ReplicateForTransfer { get; protected set; }
        public PropertyDataType Type { get; protected set; }                   // A Property/PropertyType.type
        public double CurveDefault { get; protected set; }
        public bool ReplicateToDatabaseAllowedOnItems { get; protected set; }
        public bool ClientOnly { get; protected set; }
        public bool SerializeEntityToPowerPayload { get; protected set; }
        public bool SerializePowerToPowerPayload { get; protected set; }
        public PrototypeId TooltipText { get; protected set; }                 // Localization/Translations/Properties/PropertyTranslation.defaults
        public bool TruncatePropertyValueToInt { get; protected set; }
        public EvalPrototype Eval { get; protected set; }                      // R Eval/Eval.defaults
        public bool EvalAlwaysCalculates { get; protected set; }
        public bool SerializeConditionSrcToCondition { get; protected set; }
        public bool ReplicateToTrader { get; protected set; }
        public PrototypeId ValueDisplayFormat { get; protected set; }          // Localization/Translations/Translation.defaults

        [DoNotCopy]
        public bool ShouldClampValue { get => Min != 0f || Max != 0f; }
    }
}
