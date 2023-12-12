namespace MHServerEmu.Games.GameData.Prototypes
{
    public class BoundsPrototype : Prototype
    {
        public BoundsCollisionType CollisionType;
        public bool BlocksSpawns;
        public bool ComplexPickingOnly;
        public bool IgnoreCollisionWithAllies;
        public bool BlocksLanding;
        public bool BlocksLineOfSight;
        public BoundsMovementPowerBlockType BlocksMovementPowers;
        public bool IgnoreBlockingWithAvatars;
        public bool BlockOnlyMyself;
        public BoundsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(BoundsPrototype), proto); }
    }

    public enum BoundsCollisionType
    {
        None,
        Overlapping,
        Blocking,
    }

    public enum BoundsMovementPowerBlockType
    {
        None,
        Ground,
        All,
    }

    public class CapsuleBoundsPrototype : BoundsPrototype
    {
        public float Radius;
        public float HeightFromCenter;
        public CapsuleBoundsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(CapsuleBoundsPrototype), proto); }
    }

    public class SphereBoundsPrototype : BoundsPrototype
    {
        public float Radius;
        public SphereBoundsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(SphereBoundsPrototype), proto); }
    }

    public class TriangleBoundsPrototype : BoundsPrototype
    {
        public float AngleDegrees;
        public float HeightFromCenter;
        public float Length;
        public TriangleBoundsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TriangleBoundsPrototype), proto); }
    }
    public class WedgeBoundsPrototype : BoundsPrototype
    {
        public float AngleDegrees;
        public float BaseWidth;
        public float Length;
        public float HeightFromCenter;
        public WedgeBoundsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(WedgeBoundsPrototype), proto); }
    }

    public class BoxBoundsPrototype : BoundsPrototype
    {
        public float Width;
        public float Length;
        public float Height;
        public bool AxisAligned;
        public BoxBoundsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(BoxBoundsPrototype), proto); }
    }
}
