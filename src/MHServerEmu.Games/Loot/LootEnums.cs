using MHServerEmu.Games.GameData.Calligraphy.Attributes;

namespace MHServerEmu.Games.Loot
{
    public enum LootRollResult
    {
        NoRoll = 0,
        Success = 1,
        Failure = 2,
        PartialSuccess = 3,
    };

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
