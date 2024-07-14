using MHServerEmu.Games.GameData.Calligraphy.Attributes;

namespace MHServerEmu.Games.Loot
{
    [Flags]
    public enum LootRollResult
    {
        NoRoll          = 0,
        Success         = 1 << 0,
        Failure         = 1 << 1,
        PartialSuccess  = Success | Failure
    }

    [Flags]
    public enum LootDropChanceModifiers
    {
        None                            = 0,
        CooldownOncePerXHours           = 1 << 0,
        CooldownOncePerRollover         = 1 << 1,
        CooldownByChannel               = 1 << 2,
        SpecialItemFind                 = 1 << 3,
        PerAccount                      = 1 << 4,
        DifficultyModeRestricted        = 1 << 5,
        RegionRestricted                = 1 << 6,
        KillCountRestricted             = 1 << 7,
        KillCountRequirementMet         = 1 << 8,
        PreviewOnly                     = 1 << 9,
        WeekdayRestricted               = 1 << 10,
        ConditionRestricted             = 1 << 11,
        DifficultyTierNoDropModified    = 1 << 12,
        DifficultyTierRestricted        = 1 << 13,
        IgnoreCooldown                  = 1 << 14,
        IgnoreCap                       = 1 << 15,
        LevelRestricted                 = 1 << 16,
        RareItemFind                    = 1 << 17,
        Modifier18                      = 1 << 18,
        IncludeCurrencyBonus            = 1 << 19,
        Modifier20                      = 1 << 20,
    }

    [Flags]
    public enum RestrictionTestFlags
    {
        None        = 0,
        Flag0       = 1 << 0,
        Flag1       = 1 << 1,
        Rarity      = 1 << 2,
        Flag3       = 1 << 3,
        Flag4       = 1 << 4,
        Flag5       = 1 << 5,
        Flag6       = 1 << 6,
        Flag7       = 1 << 7,
        Flag8       = 1 << 8,
        Cooldown    = 1 << 9,
        All = Flag0 | Flag1 | Rarity | Flag3 | Flag4 | Flag5 | Flag6 | Flag7 | Flag8 | Cooldown
    }

    [AssetEnum((int)None)]
    [Flags]
    public enum LootContext
    {
        None                = 0,
        AchievementReward   = 1 << 0,
        LeaderboardReward   = 1 << 1,
        CashShop            = 1 << 2,
        Crafting            = 1 << 3,
        Drop                = 1 << 4,
        Initialization      = 1 << 5,
        Vendor              = 1 << 6,
        MissionReward       = 1 << 7,
        MysteryChest        = 1 << 8,
    }

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
    public enum LootDropEventType
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

    [AssetEnum]
    public enum LootBindingType
    {
        None = 0,
        TradeRestricted = 1,
        TradeRestrictedRemoveBinding = 2,
        Avatar = 3,
    }

    [AssetEnum((int)Invalid)]
    public enum EquipmentInvUISlot
    {
        Invalid = -1,
        Costume = 0,
        Gear01 = 1,
        Gear02 = 2,
        Gear03 = 3,
        Gear04 = 4,
        Gear05 = 5,
        Artifact01 = 6,
        Medal = 7,
        Artifact02 = 8,
        Relic = 9,
        Insignia = 10,
        Artifact03 = 11,
        Ring = 12,
        Legendary = 13,
        Artifact04 = 14,
        UruForged = 15,
        _16 = 16,
        Misc = 17,
        CostumeCore = 18,
        InteractiveVisual = 19,
        Pet = 20,
        Crafting = 21,
        Consumables = 22,
        Boxes = 23,
        CategoryGear = 24,
        CategoryArtifacts = 25,
        CategoryOther = 26
    }
}
