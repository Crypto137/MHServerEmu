using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class IPoint2Prototype : Prototype
    {
        public int X { get; protected set; }
        public int Y { get; protected set; }

        public Vector2 ToIPoint2()
        {
            return new(X, Y);
        }
    }
    public class Vector2Prototype : Prototype
    {
        public float X { get; protected set; }
        public float Y { get; protected set; }

        public Vector3 ToVector3()
        {
            return new(X, Y, 0.0f);
        }
    }

    public class Vector3Prototype : Prototype
    {
        public float X { get; protected set; }
        public float Y { get; protected set; }
        public float Z { get; protected set; }
    }

    public class Rotator3Prototype : Prototype
    {
        public float Yaw { get; protected set; }
        public float Pitch { get; protected set; }
        public float Roll { get; protected set; }
    }

    public class ContextPrototype : Prototype
    {
    }

    public class TranslationPrototype : Prototype
    {
        public LocaleStringId Value { get; protected set; }
    }

    public class LocomotorPrototype : Prototype
    {
        public float Height { get; protected set; }
        public float Speed { get; protected set; }
        public float RotationSpeed { get; protected set; }
        public bool WalkEnabled { get; protected set; }
        public float WalkSpeed { get; protected set; }
        public bool Immobile { get; protected set; }
        public bool DisableOrientationForSyncMove { get; protected set; }
    }

}
