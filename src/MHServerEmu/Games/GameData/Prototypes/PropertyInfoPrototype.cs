using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PropertyInfoPrototype
    {
        public PropertyPrototype Mixin { get; set; }                   // contains mixin param information
        public long Version { get; }
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


        public PropertyInfoPrototype(Prototype prototype)
        {
            foreach (PrototypeEntryElement element in prototype.Entries[0].Elements)
            {
                switch (GameDatabase.GetBlueprintFieldName(element.Id))
                {
                    case nameof(AggMethod):
                        // AggMethod is null for some properties
                        string aggMethod = GameDatabase.GetAssetName((ulong)element.Value);
                        if (aggMethod == string.Empty) continue;
                        AggMethod = (AggregationMethod)Enum.Parse(typeof(AggregationMethod), aggMethod);
                        break;
                    case nameof(ClientOnly):
                        ClientOnly = (bool)element.Value;
                        break;
                    case nameof(CurveDefault):
                        CurveDefault = (double)element.Value;
                        break;
                    case nameof(Eval):
                        Eval = element.Value;
                        break;
                    case nameof(EvalAlwaysCalculates):
                        EvalAlwaysCalculates = (bool)element.Value;
                        break;
                    case nameof(Min):
                        Min = (double)element.Value;
                        break;
                    case nameof(Max):
                        Max = (double)element.Value;
                        break;
                    case nameof(ReplicateForTransfer):
                        ReplicateForTransfer = (bool)element.Value;
                        break;
                    case nameof(ReplicateToDatabase):
                        ReplicateToDatabase = (DatabasePolicy)Enum.Parse(typeof(DatabasePolicy), GameDatabase.GetAssetName((ulong)element.Value));
                        break;
                    case nameof(ReplicateToDatabaseAllowedOnItems):
                        ReplicateToDatabaseAllowedOnItems = (bool)element.Value;
                        break;
                    case nameof(ReplicateToOwner):
                        ReplicateToOwner = (bool)element.Value;
                        break;
                    case nameof(ReplicateToParty):
                        ReplicateToParty = (bool)element.Value;
                        break;
                    case nameof(ReplicateToProximity):
                        ReplicateToProximity = (bool)element.Value;
                        break;
                    case nameof(ReplicateToDiscovery):
                        ReplicateToDiscovery = (bool)element.Value;
                        break;
                    case nameof(ReplicateToTrader):
                        ReplicateToTrader = (bool)element.Value;
                        break;
                    case nameof(ValueDisplayFormat):
                        ValueDisplayFormat = (ulong)element.Value;
                        break;
                    case nameof(SerializeEntityToPowerPayload):
                        SerializeEntityToPowerPayload = (bool)element.Value;
                        break;
                    case nameof(SerializePowerToPowerPayload):
                        SerializePowerToPowerPayload = (bool)element.Value;
                        break;
                    case nameof(SerializeConditionSrcToCondition):
                        SerializeConditionSrcToCondition = (bool)element.Value;
                        break;
                    case nameof(TooltipText):
                        TooltipText = (ulong)element.Value;
                        break;
                    case nameof(TruncatePropertyValueToInt):
                        TruncatePropertyValueToInt = (bool)element.Value;
                        break;
                    case nameof(Type):
                        Type = (PropertyType)Enum.Parse(typeof(PropertyType), GameDatabase.GetAssetName((ulong)element.Value));
                        break;
                    case nameof(Version):
                        Version = (long)element.Value;
                        break;
                }
            }
        }
    }
    // TODO: Delete repeated enums
    public enum DBPolicyAssetEnum
    {
        UseParent = -4,
        PerField = -3,
        PropertyCollection = -2,
        Invalid = -1,
        None = 0,
        Frequent = 1,
        Infrequent = 1,
        PlayerLargeBlob = 2,
    }
    public enum PropertyDataType
    {
        Boolean = 0,
        Real = 1,
        Integer = 2,
        Prototype = 3,
        Curve = 4,
        Asset = 5,
        EntityId = 6,
        Time = 7,
        Guid = 8,
        RegionId = 9,
        Int21Vector3 = 10,
    }

    public enum PropertyAggregationMethod
    {
        None = 0,
        Min = 1,
        Max = 2,
        Sum = 3,
        Mul = 4,
        Set = 5,
    }

    // TODO: Fix Conflicts

    public class PropertyInfoPrototype2 : Prototype
    {
        public sbyte Version;
        public PropertyAggregationMethod AggMethod;
        public float Min;
        public float Max;
        public DBPolicyAssetEnum ReplicateToDatabase;
        public bool ReplicateToProximity;
        public bool ReplicateToParty;
        public bool ReplicateToOwner;
        public bool ReplicateToDiscovery;
        public bool ReplicateForTransfer;
        public PropertyDataType Type;
        public float CurveDefault;
        public bool ReplicateToDatabaseAllowedOnItems;
        public bool ClientOnly;
        public bool SerializeEntityToPowerPayload;
        public bool SerializePowerToPowerPayload;
        public ulong TooltipText;
        public bool TruncatePropertyValueToInt;
        public EvalPrototype Eval;
        public bool EvalAlwaysCalculates;
        public bool SerializeConditionSrcToCondition;
        public bool ReplicateToTrader;
        public ulong ValueDisplayFormat;
        public PropertyInfoPrototype2(Prototype proto) : base(proto) { FillPrototype(typeof(PropertyInfoPrototype2), proto); }
    }
}
