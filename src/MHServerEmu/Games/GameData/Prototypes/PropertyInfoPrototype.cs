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
        public object Eval { get; protected set; }                             // R Eval/Eval.defaults
        public bool EvalAlwaysCalculates { get; protected set; }
        public bool SerializeConditionSrcToCondition { get; protected set; }
        public bool ReplicateToTrader { get; protected set; }
        public PrototypeId ValueDisplayFormat { get; protected set; }          // Localization/Translations/Translation.defaults

        public void FillPropertyInfoFields()
        {
            // temp method for compatibility
            foreach (PrototypeSimpleField field in FieldGroups[0].SimpleFields)
            {
                switch (GameDatabase.GetBlueprintFieldName(field.Id))
                {
                    case nameof(AggMethod):
                        // AggMethod is null for some properties
                        string aggMethod = GameDatabase.GetAssetName((StringId)field.Value);
                        if (aggMethod == string.Empty) continue;
                        AggMethod = Enum.Parse<AggregationMethod>(aggMethod);
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
                        ReplicateToDatabase = Enum.Parse<DatabasePolicy>(GameDatabase.GetAssetName((StringId)field.Value));
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
                        Type = Enum.Parse<PropertyDataType>(GameDatabase.GetAssetName((StringId)field.Value));
                        break;
                    case nameof(Version):
                        Version = (long)field.Value;
                        break;
                }
            }
        }
    }
}
