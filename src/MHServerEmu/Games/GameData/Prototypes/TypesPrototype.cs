using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.GameData.Prototypes
{
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
