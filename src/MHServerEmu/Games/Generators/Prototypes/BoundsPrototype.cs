using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class BoundsPrototype : Prototype
    {
        public bool BlockOnlyMyself;
        public bool BlocksLanding;
        public bool BlocksLineOfSight;
        public bool BlocksSpawns;
        public BoundsCollision CollisionType;
        public bool ComplexPickingOnly;
        public bool IgnoreCollisionWithAllies;
        public BoundsMovementPowerBlock BlocksMovementPowers;
        public bool IgnoreBlockingWithAvatars;
        public BoundsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(BoundsPrototype), proto); }
    }

    public enum BoundsCollision {
        None,
	    Overlapping,
	    Blocking,
    }

    public enum BoundsMovementPowerBlock {
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

    public class BoxBoundsPrototype : BoundsPrototype
    {
        public float Width;
        public float Length;
        public float Height;
        public bool AxisAligned;
        public BoxBoundsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(BoxBoundsPrototype), proto); }
    }
}
