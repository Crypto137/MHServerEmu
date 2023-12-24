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
        public BoundsCollisionType CollisionType { get; private set; }
        public bool BlocksSpawns { get; private set; }
        public bool ComplexPickingOnly { get; private set; }
        public bool IgnoreCollisionWithAllies { get; private set; }
        public bool BlocksLanding { get; private set; }
        public bool BlocksLineOfSight { get; private set; }
        public BoundsMovementPowerBlockType BlocksMovementPowers { get; private set; }
        public bool IgnoreBlockingWithAvatars { get; private set; }
        public bool BlockOnlyMyself { get; private set; }
    }

    public class CapsuleBoundsPrototype : BoundsPrototype
    {
        public float Radius { get; private set; }
        public float HeightFromCenter { get; private set; }
    }

    public class SphereBoundsPrototype : BoundsPrototype
    {
        public float Radius { get; private set; }
    }

    public class TriangleBoundsPrototype : BoundsPrototype
    {
        public float AngleDegrees { get; private set; }
        public float Length { get; private set; }
        public float HeightFromCenter { get; private set; }
    }

    public class WedgeBoundsPrototype : BoundsPrototype
    {
        public float AngleDegrees { get; private set; }
        public float BaseWidth { get; private set; }
        public float Length { get; private set; }
        public float HeightFromCenter { get; private set; }
    }

    public class BoxBoundsPrototype : BoundsPrototype
    {
        public float Width { get; private set; }
        public float Length { get; private set; }
        public float Height { get; private set; }
        public bool AxisAligned { get; private set; }
    }
}
