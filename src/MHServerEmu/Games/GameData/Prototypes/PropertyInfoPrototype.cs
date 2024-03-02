using MHServerEmu.Common.Logging;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PropertyInfoPrototype : Prototype
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

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
        public AOINetworkPolicyValues RepNetwork { get; private set; } = AOINetworkPolicyValues.AOIChannelNone;
        [DoNotCopy]
        public bool ShouldClampValue { get => Min != 0f || Max != 0f; }

        public override void PostProcess()
        {
            base.PostProcess();

            // Reconstruct AOI network policy
            if (ReplicateToProximity)
                RepNetwork |= AOINetworkPolicyValues.AOIChannelProximity;

            if (ReplicateToParty)
                RepNetwork |= AOINetworkPolicyValues.AOIChannelParty;

            if (ReplicateToOwner)
                RepNetwork |= AOINetworkPolicyValues.AOIChannelOwner;

            if (ReplicateToTrader)
                RepNetwork |= AOINetworkPolicyValues.AOIChannelTrader;

            if (ReplicateToDiscovery)
                RepNetwork |= AOINetworkPolicyValues.AOIChannelDiscovery;

            // Validation messages based on PropertyInfoPrototype::Validate()
            if (ClientOnly)
            {
                if (RepNetwork != AOINetworkPolicyValues.AOIChannelNone)
                    Logger.Warn("PostProcess(): Client-only properties cannot have any network replication policies");

                RepNetwork = AOINetworkPolicyValues.AOIChannelClientOnly;
            }

            if (TruncatePropertyValueToInt)
            {
                if ((Type == PropertyDataType.Real || Type == PropertyDataType.Curve) == false)
                {
                    Logger.Warn("PostProcess(): TruncatePropertyValueToInt should only be set to True for Float or Curve properties");
                    TruncatePropertyValueToInt = false;
                }
            }
        }
    }
}
