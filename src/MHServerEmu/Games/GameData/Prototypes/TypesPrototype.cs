namespace MHServerEmu.Games.GameData.Prototypes
{
    public class IPoint2Prototype : Prototype
    {
        public int X { get; private set; }
        public int Y { get; private set; }
    }

    public class Vector2Prototype : Prototype
    {
        public float X { get; private set; }
        public float Y { get; private set; }
    }

    public class Vector3Prototype : Prototype
    {
        public float X { get; private set; }
        public float Y { get; private set; }
        public float Z { get; private set; }
    }

    public class Rotator3Prototype : Prototype
    {
        public float Yaw { get; private set; }
        public float Pitch { get; private set; }
        public float Roll { get; private set; }
    }

    public class ContextPrototype : Prototype
    {
    }

    public class TranslationPrototype : Prototype
    {
        public ulong Value { get; private set; }
    }

    public class LocomotorPrototype : Prototype
    {
        public float Height { get; private set; }
        public float Speed { get; private set; }
        public float RotationSpeed { get; private set; }
        public bool WalkEnabled { get; private set; }
        public float WalkSpeed { get; private set; }
        public bool Immobile { get; private set; }
        public bool DisableOrientationForSyncMove { get; private set; }
    }

    #region KeywordPrototype

    public class KeywordPrototype : Prototype
    {
        public ulong IsAKeyword { get; private set; }
    }

    public class EntityKeywordPrototype : KeywordPrototype
    {
        public ulong DisplayName { get; private set; }
    }

    public class MobKeywordPrototype : EntityKeywordPrototype
    {
    }

    public class AvatarKeywordPrototype : EntityKeywordPrototype
    {
    }

    public class MissionKeywordPrototype : KeywordPrototype
    {
    }

    public class PowerKeywordPrototype : KeywordPrototype
    {
        public ulong DisplayName { get; private set; }
        public bool DisplayInPowerKeywordsList { get; private set; }
    }

    public class RankKeywordPrototype : KeywordPrototype
    {
    }

    public class RegionKeywordPrototype : KeywordPrototype
    {
    }

    public class AffixCategoryPrototype : KeywordPrototype
    {
    }

    public class FulfillablePrototype : Prototype
    {
    }

    #endregion
}
