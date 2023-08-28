using MHServerEmu.GameServer.GameData.Gpak.FileFormats;
using MHServerEmu.GameServer.Properties;

namespace MHServerEmu.GameServer.GameData.Prototypes
{
    public class PropertyInfoPrototype
    {
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
                    case "Eval":
                        Eval = element.Value;
                        break;
                    case "SerializePowerToPowerPayload":
                        SerializePowerToPowerPayload = (bool)element.Value;
                        break;
                    case "SerializeConditionSrcToCondition":
                        SerializeConditionSrcToCondition = (bool)element.Value;
                        break;
                    case "Type":
                        Type = (PropertyType)Enum.Parse(typeof(PropertyType), GameDatabase.Calligraphy.AssetDict[(ulong)element.Value]);
                        break;
                    case "ReplicateToTrader":
                        ReplicateToTrader = (bool)element.Value;
                        break;
                    case "ReplicateToDatabase": // NOTE: enum values here are wrong
                        ReplicateToDatabase = (DatabasePolicy)Enum.Parse(typeof(DatabasePolicy), GameDatabase.Calligraphy.AssetDict[(ulong)element.Value]);
                        break;
                    case "ClientOnly":
                        ClientOnly = (bool)element.Value;
                        break;
                    case "ReplicateToProximity":
                        ReplicateToProximity = (bool)element.Value;
                        break;
                    case "TruncatePropertyValueToInt":
                        TruncatePropertyValueToInt = (bool)element.Value;
                        break;
                    case "ReplicateToOwner":
                        ReplicateToOwner = (bool)element.Value;
                        break;
                    case "ValueDisplayFormat":
                        ValueDisplayFormat = (ulong)element.Value;
                        break;
                    case "ReplicateToDatabaseAllowedOnItems":
                        ReplicateToDatabaseAllowedOnItems = (bool)element.Value;
                        break;
                    case "CurveDefault":
                        CurveDefault = (double)element.Value;
                        break;
                    case "SerializeEntityToPowerPayload":
                        SerializeEntityToPowerPayload = (bool)element.Value;
                        break;
                    case "EvalAlwaysCalculates":
                        EvalAlwaysCalculates = (bool)element.Value;
                        break;
                    case "ReplicateToParty":
                        ReplicateToParty = (bool)element.Value;
                        break;
                    case "ReplicateForTransfer":
                        ReplicateForTransfer = (bool)element.Value;
                        break;
                    case "AggMethod":
                        AggMethod = (AggregationMethod)Enum.Parse(typeof(AggregationMethod), GameDatabase.Calligraphy.AssetDict[(ulong)element.Value]);
                        break;
                    case "Min":
                        Min = (double)element.Value;
                        break;
                    case "Max":
                        Max = (double)element.Value;
                        break;
                    case "TooltipText":
                        TooltipText = (ulong)element.Value;
                        break;
                    case "ReplicateToDiscovery":
                        ReplicateToDiscovery = (bool)element.Value;
                        break;
                    case "Version":
                        Version = (long)element.Value;
                        break;
                }
            }
        }
    }
}
