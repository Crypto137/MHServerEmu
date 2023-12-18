using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PropertyInfoPrototype : Prototype
    {
        public long Version { get; }
        public AggregationMethod AggMethod { get; }             // A Property/AggregationMethod.type
        public double Min { get; }
        public double Max { get; }
        public DatabasePolicy ReplicateToDatabase { get; }      // A Property/DatabasePolicy.type
        public bool ReplicateToProximity { get; }
        public bool ReplicateToParty { get; }
        public bool ReplicateToOwner { get; }
        public bool ReplicateToDiscovery { get; }
        public bool ReplicateForTransfer { get; }
        public PropertyDataType Type { get; }                   // A Property/PropertyType.type
        public double CurveDefault { get; }
        public bool ReplicateToDatabaseAllowedOnItems { get; }
        public bool ClientOnly { get; }
        public bool SerializeEntityToPowerPayload { get; }
        public bool SerializePowerToPowerPayload { get; }
        public PrototypeId TooltipText { get; }                 // Localization/Translations/Properties/PropertyTranslation.defaults
        public bool TruncatePropertyValueToInt { get; }
        public object Eval { get; }                             // R Eval/Eval.defaults
        public bool EvalAlwaysCalculates { get; }
        public bool SerializeConditionSrcToCondition { get; }
        public bool ReplicateToTrader { get; }
        public PrototypeId ValueDisplayFormat { get; }          // Localization/Translations/Translation.defaults

        public PropertyInfoPrototype(BinaryReader reader) : base(reader)
        {
            // NOTE: Old misguided experiments below

            foreach (PrototypeSimpleField field in FieldGroups[0].SimpleFields)
            {
                switch (GameDatabase.GetBlueprintFieldName(field.Id))
                {
                    case nameof(AggMethod):
                        // AggMethod is null for some properties
                        string aggMethod = GameDatabase.GetAssetName((StringId)field.Value);
                        if (aggMethod == string.Empty) continue;
                        AggMethod = (AggregationMethod)Enum.Parse(typeof(AggregationMethod), aggMethod);
                        break;
                    case nameof(ClientOnly):
                        ClientOnly = (bool)field.Value;
                        break;
                    case nameof(CurveDefault):
                        CurveDefault = (double)field.Value;
                        break;
                    case nameof(Eval):
                        Eval = field.Value;
                        break;
                    case nameof(EvalAlwaysCalculates):
                        EvalAlwaysCalculates = (bool)field.Value;
                        break;
                    case nameof(Min):
                        Min = (double)field.Value;
                        break;
                    case nameof(Max):
                        Max = (double)field.Value;
                        break;
                    case nameof(ReplicateForTransfer):
                        ReplicateForTransfer = (bool)field.Value;
                        break;
                    case nameof(ReplicateToDatabase):
                        ReplicateToDatabase = (DatabasePolicy)Enum.Parse(typeof(DatabasePolicy), GameDatabase.GetAssetName((StringId)field.Value));
                        break;
                    case nameof(ReplicateToDatabaseAllowedOnItems):
                        ReplicateToDatabaseAllowedOnItems = (bool)field.Value;
                        break;
                    case nameof(ReplicateToOwner):
                        ReplicateToOwner = (bool)field.Value;
                        break;
                    case nameof(ReplicateToParty):
                        ReplicateToParty = (bool)field.Value;
                        break;
                    case nameof(ReplicateToProximity):
                        ReplicateToProximity = (bool)field.Value;
                        break;
                    case nameof(ReplicateToDiscovery):
                        ReplicateToDiscovery = (bool)field.Value;
                        break;
                    case nameof(ReplicateToTrader):
                        ReplicateToTrader = (bool)field.Value;
                        break;
                    case nameof(ValueDisplayFormat):
                        ValueDisplayFormat = (PrototypeId)field.Value;
                        break;
                    case nameof(SerializeEntityToPowerPayload):
                        SerializeEntityToPowerPayload = (bool)field.Value;
                        break;
                    case nameof(SerializePowerToPowerPayload):
                        SerializePowerToPowerPayload = (bool)field.Value;
                        break;
                    case nameof(SerializeConditionSrcToCondition):
                        SerializeConditionSrcToCondition = (bool)field.Value;
                        break;
                    case nameof(TooltipText):
                        TooltipText = (PrototypeId)field.Value;
                        break;
                    case nameof(TruncatePropertyValueToInt):
                        TruncatePropertyValueToInt = (bool)field.Value;
                        break;
                    case nameof(Type):
                        Type = (PropertyDataType)Enum.Parse(typeof(PropertyDataType), GameDatabase.GetAssetName((StringId)field.Value));
                        break;
                    case nameof(Version):
                        Version = (long)field.Value;
                        break;
                }
            }
        }
    }
}
