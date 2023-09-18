using MHServerEmu.GameServer.GameData.Gpak.FileFormats;
using MHServerEmu.GameServer.Properties;

namespace MHServerEmu.GameServer.GameData.Prototypes
{
    public class PropertyInfoPrototype
    {
        public PropertyPrototype Mixin { get; set; }                   // contains mixin param information

        public object Eval { get; }                             // R
        public bool SerializePowerToPowerPayload { get; }
        public bool SerializeConditionSrcToCondition { get; }
        public PropertyType Type { get; }
        public bool ReplicateToTrader { get; }
        public DatabasePolicy ReplicateToDatabase { get; }
        public bool ClientOnly { get; }
        public bool ReplicateToProximity { get; }
        public bool TruncatePropertyValueToInt { get; }
        public bool ReplicateToOwner { get; }
        public ulong ValueDisplayFormat { get; }                // P
        public bool ReplicateToDatabaseAllowedOnItems { get; }
        public double CurveDefault { get; }
        public bool SerializeEntityToPowerPayload { get; }
        public bool EvalAlwaysCalculates { get; }
        public bool ReplicateToParty { get; }
        public bool ReplicateForTransfer { get; }
        public AggregationMethod AggMethod { get; }
        public double Min { get; }
        public double Max { get; }
        public ulong TooltipText { get; }                       // P
        public bool ReplicateToDiscovery { get; }
        public long Version { get; }

        public PropertyInfoPrototype(Prototype prototype)
        {
            foreach (PrototypeDataEntryElement element in prototype.Data.Entries[0].Elements)
            {
                switch (GameDatabase.Calligraphy.PrototypeFieldDict[element.Id])
                {
                    case nameof(Eval):
                        Eval = element.Value;
                        break;
                    case nameof(SerializePowerToPowerPayload):
                        SerializePowerToPowerPayload = (bool)element.Value;
                        break;
                    case nameof(SerializeConditionSrcToCondition):
                        SerializeConditionSrcToCondition = (bool)element.Value;
                        break;
                    case nameof(Type):
                        Type = (PropertyType)Enum.Parse(typeof(PropertyType), GameDatabase.Calligraphy.AssetDict[(ulong)element.Value]);
                        break;
                    case nameof(ReplicateToTrader):
                        ReplicateToTrader = (bool)element.Value;
                        break;
                    case nameof(ReplicateToDatabase):
                        ReplicateToDatabase = (DatabasePolicy)Enum.Parse(typeof(DatabasePolicy), GameDatabase.Calligraphy.AssetDict[(ulong)element.Value]);
                        break;
                    case nameof(ClientOnly):
                        ClientOnly = (bool)element.Value;
                        break;
                    case nameof(ReplicateToProximity):
                        ReplicateToProximity = (bool)element.Value;
                        break;
                    case nameof(TruncatePropertyValueToInt):
                        TruncatePropertyValueToInt = (bool)element.Value;
                        break;
                    case nameof(ReplicateToOwner):
                        ReplicateToOwner = (bool)element.Value;
                        break;
                    case nameof(ValueDisplayFormat):
                        ValueDisplayFormat = (ulong)element.Value;
                        break;
                    case nameof(ReplicateToDatabaseAllowedOnItems):
                        ReplicateToDatabaseAllowedOnItems = (bool)element.Value;
                        break;
                    case nameof(CurveDefault):
                        CurveDefault = (double)element.Value;
                        break;
                    case nameof(SerializeEntityToPowerPayload):
                        SerializeEntityToPowerPayload = (bool)element.Value;
                        break;
                    case nameof(EvalAlwaysCalculates):
                        EvalAlwaysCalculates = (bool)element.Value;
                        break;
                    case nameof(ReplicateToParty):
                        ReplicateToParty = (bool)element.Value;
                        break;
                    case nameof(ReplicateForTransfer):
                        ReplicateForTransfer = (bool)element.Value;
                        break;
                    case nameof(AggMethod):
                        AggMethod = (AggregationMethod)Enum.Parse(typeof(AggregationMethod), GameDatabase.Calligraphy.AssetDict[(ulong)element.Value]);
                        break;
                    case nameof(Min):
                        Min = (double)element.Value;
                        break;
                    case nameof(Max):
                        Max = (double)element.Value;
                        break;
                    case nameof(TooltipText):
                        TooltipText = (ulong)element.Value;
                        break;
                    case nameof(ReplicateToDiscovery):
                        ReplicateToDiscovery = (bool)element.Value;
                        break;
                    case nameof(Version):
                        Version = (long)element.Value;
                        break;
                }
            }
        }
    }
}
