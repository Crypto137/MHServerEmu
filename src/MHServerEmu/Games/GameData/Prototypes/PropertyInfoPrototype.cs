using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PropertyInfoPrototype
    {
        public PropertyPrototype Mixin { get; set; }                   // contains mixin param information

        public AggregationMethod AggMethod { get; }
        public bool ClientOnly { get; }
        public double CurveDefault { get; }
        public object Eval { get; }                             // R
        public bool EvalAlwaysCalculates { get; }
        public double Min { get; }
        public double Max { get; }
        public bool ReplicateForTransfer { get; }
        public DatabasePolicy ReplicateToDatabase { get; }
        public bool ReplicateToDatabaseAllowedOnItems { get; }
        public bool ReplicateToOwner { get; }
        public bool ReplicateToParty { get; }
        public bool ReplicateToProximity { get; }
        public bool ReplicateToDiscovery { get; }
        public bool ReplicateToTrader { get; }
        public ulong ValueDisplayFormat { get; }                // P
        public bool SerializeEntityToPowerPayload { get; }
        public bool SerializePowerToPowerPayload { get; }
        public bool SerializeConditionSrcToCondition { get; }
        public ulong TooltipText { get; }                       // P
        public bool TruncatePropertyValueToInt { get; }
        public PropertyType Type { get; }
        public long Version { get; }

        public PropertyInfoPrototype(Prototype prototype)
        {
            foreach (PrototypeSimpleField field in prototype.FieldGroups[0].SimpleFields)
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
                        ValueDisplayFormat = (ulong)field.Value;
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
                        TooltipText = (ulong)field.Value;
                        break;
                    case nameof(TruncatePropertyValueToInt):
                        TruncatePropertyValueToInt = (bool)field.Value;
                        break;
                    case nameof(Type):
                        Type = (PropertyType)Enum.Parse(typeof(PropertyType), GameDatabase.GetAssetName((StringId)field.Value));
                        break;
                    case nameof(Version):
                        Version = (long)field.Value;
                        break;
                }
            }
        }
    }
}
