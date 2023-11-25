namespace MHServerEmu.Games.GameData.Prototypes
{
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

    public class BoundsPrototype : Prototype
    {
        public BoundsCollisionType CollisionType { get; set; }
        public bool BlocksSpawns { get; set; }
        public bool ComplexPickingOnly { get; set; }
        public bool IgnoreCollisionWithAllies { get; set; }
        public bool BlocksLanding { get; set; }
        public bool BlocksLineOfSight { get; set; }
        public BoundsMovementPowerBlockType BlocksMovementPowers { get; set; }
        public bool IgnoreBlockingWithAvatars { get; set; }
        public bool BlockOnlyMyself { get; set; }
    }

    public class CapsuleBoundsPrototype : BoundsPrototype
    {
        public float Radius { get; set; }
        public float HeightFromCenter { get; set; }
    }

    public class SphereBoundsPrototype : BoundsPrototype
    {
        public float Radius { get; set; }
    }

    public class TriangleBoundsPrototype : BoundsPrototype
    {
        public float AngleDegrees { get; set; }
        public float Length { get; set; }
        public float HeightFromCenter { get; set; }
    }

    public class WedgeBoundsPrototype : BoundsPrototype
    {
        public float AngleDegrees { get; set; }
        public float BaseWidth { get; set; }
        public float Length { get; set; }
        public float HeightFromCenter { get; set; }
    }

    public class BoxBoundsPrototype : BoundsPrototype
    {
        public float Width { get; set; }
        public float Length { get; set; }
        public float Height { get; set; }
        public bool AxisAligned { get; set; }
    }
}
