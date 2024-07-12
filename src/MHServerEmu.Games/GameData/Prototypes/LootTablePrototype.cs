using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.GameData.LiveTuning;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    public enum LootEventType   // Loot/LootDropEventType.type
    {
        None = 0,
        OnInteractedWith = 3,
        OnHealthBelowPct = 2,
        OnHealthBelowPctHit = 1,
        OnKilled = 4,
        OnKilledChampion = 5,
        OnKilledElite = 6,
        OnKilledMiniBoss = 7,
        OnHit = 8,
        OnDamagedForPctHealth = 9,
    }

    [AssetEnum((int)None)]
    public enum LootActionType
    {
        None = 0,
        Spawn = 1,
        Give = 2
    }

    [AssetEnum]
    public enum CharacterFilterType
    {
        None = 0,
        DropCurrentAvatarOnly = 1,
        DropUnownedAvatarOnly = 2,
    }

    [AssetEnum((int)CurrentRecipientOnly)]
    public enum PlayerScope
    {
        CurrentRecipientOnly = 0,
        Party = 1,
        Nearby = 2,
        Friends = 3,
        Guild = 4,
    }

    [AssetEnum((int)None)]
    public enum AffixPosition
    {
        None = 0,
        Prefix = 1,
        Suffix = 2,
        Visual = 3,
        Cosmic = 5,
        Unique = 6,
        Ultimate = 4,
        Blessing = 7,
        Runeword = 8,
        TeamUp = 9,
        Metadata = 10,
        PetTech1 = 11,
        PetTech2 = 12,
        PetTech3 = 13,
        PetTech4 = 14,
        PetTech5 = 15,
        RegionAffix = 16,
        Socket1 = 17,
        Socket2 = 18,
        Socket3 = 19,
    }

    [AssetEnum((int)All)]
    public enum Weekday
    {
        Sunday = 0,
        Monday = 1,
        Tuesday = 2,
        Wednesday = 3,
        Thursday = 4,
        Friday = 5,
        Saturday = 6,
        All = 7,
    }

    [AssetEnum]
    public enum LootBindingType
    {
        None = 0,
        TradeRestricted = 1,
        TradeRestrictedRemoveBinding = 2,
        Avatar = 3,
    }

    #endregion

    public class LootNodePrototype : Prototype
    {
        public short Weight { get; protected set; }
        public LootRollModifierPrototype[] Modifiers { get; protected set; }
    }

    public class LootDropPrototype : LootNodePrototype
    {
        public short NumMin { get; protected set; }
        public short NumMax { get; protected set; }
    }

    public class LootTablePrototype : LootDropPrototype
    {
        public PickMethod PickMethod { get; protected set; }
        public float NoDropPercent { get; protected set; }
        public LootNodePrototype[] Choices { get; protected set; }
        public LocaleStringId MissionLogRewardsText { get; protected set; }
        public bool LiveTuningDefaultEnabled { get; protected set; }

        [DoNotCopy]
        public int LootTablePrototypeEnumValue { get; private set; }

        public override void PostProcess()
        {
            base.PostProcess();

            NoDropPercent = Math.Clamp(NoDropPercent, 0f, 1f);

            LootTablePrototypeEnumValue = GetEnumValueFromBlueprint(LiveTuningData.GetLootTableBlueprintDataRef());
        }
    }

    public class LootTableAssignmentPrototype : Prototype
    {
        public AssetId Name { get; protected set; }
        public LootDropEventType Event { get; protected set; }
        public PrototypeId Table { get; protected set; }
    }
}
