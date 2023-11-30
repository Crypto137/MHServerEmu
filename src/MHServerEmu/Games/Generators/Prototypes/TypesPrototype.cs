using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class PropertyInfoPrototype : Prototype
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
        public PropertyInfoPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PropertyInfoPrototype), proto); }
    }
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

    public class IPoint2Prototype : Prototype
    {
        public int X;
        public int Y;

        public IPoint2Prototype(Prototype proto) : base(proto) { FillPrototype(typeof(IPoint2Prototype), proto); }

        public Vector2 ToIPoint2()
        {
            return new(X, Y);
        }
    }
    public class Vector2Prototype : Prototype
    {
        public float X;
        public float Y;
        public Vector2Prototype(Prototype proto) : base(proto) { FillPrototype(typeof(Vector2Prototype), proto); }
    }

    public class Vector3Prototype : Prototype
    {
        public float X;
        public float Y;
        public float Z;
        public Vector3Prototype(Prototype proto) : base(proto) { FillPrototype(typeof(Vector3Prototype), proto); }
    }

    public class Rotator3Prototype : Prototype
    {
        public float Yaw;
        public float Pitch;
        public float Roll;
        public Rotator3Prototype(Prototype proto) : base(proto) { FillPrototype(typeof(Rotator3Prototype), proto); }
    }

    public class ContextPrototype : Prototype
    {
        public ContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ContextPrototype), proto); }
    }

    public class TranslationPrototype : Prototype
    {
        public ulong Value;
        public TranslationPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TranslationPrototype), proto); }

    }

    public class LocomotorPrototype : Prototype
    {
        public float Height;
        public float Speed;
        public float RotationSpeed;
        public bool WalkEnabled;
        public float WalkSpeed;
        public bool Immobile;
        public bool DisableOrientationForSyncMove;
        public LocomotorPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LocomotorPrototype), proto); }
    }       

    public class FulfillablePrototype : Prototype
    {
        public FulfillablePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(FulfillablePrototype), proto); }
    }
}
