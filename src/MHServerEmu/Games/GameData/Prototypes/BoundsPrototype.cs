using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    public enum BoundsCollisionType
    {
        None,
        Overlapping,
        Blocking,
    }

    [AssetEnum]
    public enum BoundsMovementPowerBlockType
    {
        None,
        Ground,
        All,
    }

    #endregion

    public class BoundsPrototype : Prototype
    {
        public BoundsCollisionType CollisionType { get; protected set; }
        public bool BlocksSpawns { get; protected set; }
        public bool ComplexPickingOnly { get; protected set; }
        public bool IgnoreCollisionWithAllies { get; protected set; }
        public bool BlocksLanding { get; protected set; }
        public bool BlocksLineOfSight { get; protected set; }
        public BoundsMovementPowerBlockType BlocksMovementPowers { get; protected set; }
        public bool IgnoreBlockingWithAvatars { get; protected set; }
        public bool BlockOnlyMyself { get; protected set; }
    }

    public class CapsuleBoundsPrototype : BoundsPrototype
    {
        public float Radius { get; protected set; }
        public float HeightFromCenter { get; protected set; }
    }

    public class SphereBoundsPrototype : BoundsPrototype
    {
        public float Radius { get; protected set; }
    }

    public class TriangleBoundsPrototype : BoundsPrototype
    {
        public float AngleDegrees { get; protected set; }
        public float Length { get; protected set; }
        public float HeightFromCenter { get; protected set; }
    }

    public class WedgeBoundsPrototype : BoundsPrototype
    {
        public float AngleDegrees { get; protected set; }
        public float BaseWidth { get; protected set; }
        public float Length { get; protected set; }
        public float HeightFromCenter { get; protected set; }
    }

    public class BoxBoundsPrototype : BoundsPrototype
    {
        public float Width { get; protected set; }
        public float Length { get; protected set; }
        public float Height { get; protected set; }
        public bool AxisAligned { get; protected set; }
    }
}
