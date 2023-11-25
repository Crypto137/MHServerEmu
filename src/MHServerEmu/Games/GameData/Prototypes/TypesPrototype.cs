namespace MHServerEmu.Games.GameData.Prototypes
{
    public class IPoint2Prototype : Prototype
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class Vector2Prototype : Prototype
    {
        public float X { get; set; }
        public float Y { get; set; }
    }

    public class Vector3Prototype : Prototype
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }

    public class Rotator3Prototype : Prototype
    {
        public float Yaw { get; set; }
        public float Pitch { get; set; }
        public float Roll { get; set; }
    }

    public class ContextPrototype : Prototype
    {
    }

    public class TranslationPrototype : Prototype
    {
        public ulong Value { get; set; }
    }

    public class LocomotorPrototype : Prototype
    {
        public float Height { get; set; }
        public float Speed { get; set; }
        public float RotationSpeed { get; set; }
        public bool WalkEnabled { get; set; }
        public float WalkSpeed { get; set; }
        public bool Immobile { get; set; }
        public bool DisableOrientationForSyncMove { get; set; }
    }

    #region KeywordPrototype

    public class KeywordPrototype : Prototype
    {
        public ulong IsAKeyword { get; set; }
    }

    public class EntityKeywordPrototype : KeywordPrototype
    {
        public ulong DisplayName { get; set; }
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
        public ulong DisplayName { get; set; }
        public bool DisplayInPowerKeywordsList { get; set; }
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
